/**
 * 接口合并 —— 将多条 CaptureRecord 聚合为 EndpointModel
 */
import type { CaptureRecord } from '../model/capture-record.js';
import type { EndpointModel, AuthType, Confidence, EndpointExample } from '../model/endpoint-model.js';
import { normalizeRequest } from './request-normalizer.js';
import { inferResponseShape } from './response-normalizer.js';
import {
  transportSignature,
  requestSignature,
  semanticSignature,
} from './endpoint-fingerprint.js';
import { mergeShapes, mergeQueryShapes } from './schema-infer.js';

/** 中间分析记录 */
interface AnalyzedRecord {
  record: CaptureRecord;
  normalizedPath: string;
  queryShape: Record<string, string>;
  bodyShape: Record<string, unknown> | null;
  responseShape: Record<string, unknown> | null;
  transport: string;
  request: string;
  semantic: string | null;
}

/** 将原始记录聚合为 EndpointModel 列表 */
export function mergeRecordsToEndpoints(records: CaptureRecord[]): EndpointModel[] {
  // 过滤掉 OPTIONS 预检请求和失败请求（无状态码）
  const validRecords = records.filter(
    (r) => r.method !== 'OPTIONS' && r.status !== null,
  );

  // 1. 归一化 + 生成签名
  const analyzed: AnalyzedRecord[] = validRecords.map((record) => {
    const norm = normalizeRequest(record.url, record.requestBody);
    const transport = transportSignature(record.method, norm.normalizedPath);
    const reqSig = requestSignature(
      record.method, norm.normalizedPath, norm.queryShape, norm.bodyShape,
    );

    let parsedBody: Record<string, unknown> | null = null;
    if (record.requestBody) {
      try { parsedBody = JSON.parse(record.requestBody); } catch { /* ignore */ }
    }

    const semantic = semanticSignature(record.method, norm.normalizedPath, parsedBody);
    const responseShape = inferResponseShape(record.responseBody);

    return {
      record,
      normalizedPath: norm.normalizedPath,
      queryShape: norm.queryShape,
      bodyShape: norm.bodyShape,
      responseShape,
      transport,
      request: reqSig,
      semantic,
    };
  });

  // 2. 按签名分组（优先用语义签名，回退到请求签名）
  const groups = new Map<string, AnalyzedRecord[]>();
  for (const item of analyzed) {
    const key = item.semantic ?? item.request;
    const group = groups.get(key) ?? [];
    group.push(item);
    groups.set(key, group);
  }

  // 3. 每组合并为一个 EndpointModel
  const endpoints: EndpointModel[] = [];

  for (const [, group] of groups) {
    const first = group[0];
    const allRecords = group.map((g) => g.record);

    // 合并 queryShape
    const queryShapes = group.map((g) => g.queryShape);
    const mergedQuery = mergeQueryShapes(queryShapes);

    // 合并 bodyShape
    const bodyShapes = group.map((g) => g.bodyShape);
    const mergedBody = mergeShapes(bodyShapes);

    // 合并 responseShape
    const responseShapes = group.map((g) => g.responseShape);
    const mergedResponse = mergeShapes(responseShapes);

    // 合并 pathParams（取第一个的结构，后续样本补充 examples）
    const firstNorm = normalizeRequest(first.record.url, first.record.requestBody);
    const pathParams = firstNorm.pathParams;
    for (const item of group.slice(1)) {
      const norm = normalizeRequest(item.record.url, item.record.requestBody);
      for (const param of norm.pathParams) {
        const existing = pathParams.find((p) => p.index === param.index);
        if (existing) {
          for (const ex of param.examples) {
            if (!existing.examples.includes(ex) && existing.examples.length < 5) {
              existing.examples.push(ex);
            }
          }
        }
      }
    }

    // 状态码集合
    const statusCodes = [...new Set(allRecords.map((r) => r.status).filter((s): s is number => s !== null))].sort();

    // 场景集合
    const scenarios = [...new Set(allRecords.map((r) => r.scenarioId))];

    // 鉴权方式推断
    const authType = detectAuthType(first.record);

    // 样本示例（最多保留 3 个）
    const examples: EndpointExample[] = allRecords.slice(0, 3).map((r) => ({
      recordId: r.id,
      scenarioId: r.scenarioId,
      url: r.url,
      status: r.status,
      requestBodyPreview: r.requestBody?.slice(0, 200) ?? null,
      responseBodyPreview: r.responseBody?.slice(0, 200) ?? null,
    }));

    // 错误模型（4xx/5xx 响应的结构）
    const errorRecords = allRecords.filter((r) => r.status !== null && r.status >= 400);
    const errorShapes = errorRecords.map((r) => inferResponseShape(r.responseBody));
    const errorModel = mergeShapes(errorShapes);

    // 置信度计算
    const confidence = computeConfidence(group);

    // requestShape 签名
    const requestShapeStr = first.request;

    const endpoint: EndpointModel = {
      method: first.record.method,
      normalizedPath: first.normalizedPath,
      transportSignature: first.transport,
      requestSignature: first.request,
      semanticSignature: first.semantic,
      pathParams,
      queryShape: mergedQuery,
      bodyShape: mergedBody,
      requestShape: requestShapeStr,
      responseShape: mergedResponse,
      authType,
      sampleCount: allRecords.length,
      statusCodes,
      scenarios,
      examples,
      errorModel,
      firstSeenAt: allRecords.reduce((min, r) => r.startedAt < min ? r.startedAt : min, allRecords[0].startedAt),
      lastSeenAt: allRecords.reduce((max, r) => r.startedAt > max ? r.startedAt : max, allRecords[0].startedAt),
      confidence,
      sampleRefs: allRecords.map((r) => r.id),
      notes: null,
    };

    endpoints.push(endpoint);
  }

  // 按 normalizedPath 排序
  endpoints.sort((a, b) => a.normalizedPath.localeCompare(b.normalizedPath));

  return endpoints;
}

/** 从请求头推断鉴权方式 */
function detectAuthType(record: CaptureRecord): AuthType {
  const headers = record.requestHeaders;
  if (!headers) return 'unknown';

  if (headers['authorization']?.startsWith('Bearer ')) return 'bearer';
  if (headers['authorization']) return 'custom-signature';
  if (headers['cookie']) return 'cookie';
  if (headers['x-auth-token'] || headers['x-csrf-token']) return 'custom-signature';

  return 'none';
}

/** 计算聚合置信度 */
function computeConfidence(group: AnalyzedRecord[]): Confidence {
  // 高置信度：多个样本 + 有语义签名 + 响应结构一致
  if (group.length >= 3 && group[0].semantic !== null) return 'high';

  // 中置信度：多个样本或有明确的 queryShape
  if (group.length >= 2) return 'medium';

  // 低置信度：单样本
  return 'low';
}
