/**
 * 请求归一化 —— 对应设计文档 7.7.1
 */
import type { PathParam } from '../model/endpoint-model.js';

/** 噪声 query 参数名（时间戳、随机数、追踪 ID 等） */
const NOISE_PARAMS = new Set([
  '_', 't', 'ts', 'timestamp', 'nonce', 'rand', 'random',
  'callback', 'jsonp', '_t', 'r', 'reqid', 'traceid',
  '_xsrf', 'v', 'ver', 'version', 'sign', 'sig',
]);

/** 归一化结果 */
export interface NormalizedRequest {
  normalizedPath: string;
  queryShape: Record<string, string>;
  bodyShape: Record<string, unknown> | null;
  pathParams: PathParam[];
}

/** 从 URL 和请求体中提取归一化后的信息 */
export function normalizeRequest(
  url: string,
  requestBody: string | null,
): NormalizedRequest {
  const parsed = new URL(url);
  const { normalizedPath, pathParams } = normalizePath(parsed.pathname);

  const queryShape: Record<string, string> = {};
  for (const [key, value] of parsed.searchParams.entries()) {
    if (!NOISE_PARAMS.has(key.toLowerCase())) {
      queryShape[key] = inferParamType(value);
    }
  }

  const bodyShape = normalizeBody(requestBody);

  return { normalizedPath, queryShape, bodyShape, pathParams };
}

/** 路径参数抽象：将明显的资源 ID 替换为占位符 */
export function normalizePath(pathname: string): { normalizedPath: string; pathParams: PathParam[] } {
  const pathParams: PathParam[] = [];
  const segments = pathname.split('/');

  const normalized = segments.map((seg, index) => {
    // 纯数字 ID（5 位以上）
    if (/^\d{5,}$/.test(seg)) {
      pathParams.push({ index, name: 'id', examples: [seg] });
      return ':id';
    }
    // 16 位以上十六进制哈希
    if (/^[a-f0-9]{16,}$/i.test(seg)) {
      pathParams.push({ index, name: 'hash', examples: [seg] });
      return ':hash';
    }
    // UUID
    if (/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(seg)) {
      pathParams.push({ index, name: 'uuid', examples: [seg] });
      return ':uuid';
    }
    // 115 分享码格式（字母数字混合 10+ 位）
    if (/^[a-z0-9]{10,}$/i.test(seg) && /[a-z]/i.test(seg) && /\d/.test(seg)) {
      pathParams.push({ index, name: 'code', examples: [seg] });
      return ':code';
    }
    return seg;
  });

  return { normalizedPath: normalized.join('/'), pathParams };
}

/** 归一化请求体：提取结构和关键语义字段 */
export function normalizeBody(body: string | null): Record<string, unknown> | null {
  if (!body) return null;

  // 尝试 JSON 解析
  try {
    const parsed = JSON.parse(body);
    if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
      return inferObjectShape(parsed as Record<string, unknown>);
    }
    if (Array.isArray(parsed)) {
      return { _type: 'array', _length: parsed.length };
    }
  } catch { /* not JSON */ }

  // 尝试 URL-encoded 解析
  if (body.includes('=')) {
    try {
      const params = new URLSearchParams(body);
      const shape: Record<string, unknown> = {};
      for (const [key, value] of params.entries()) {
        shape[key] = inferParamType(value);
      }
      if (Object.keys(shape).length > 0) return shape;
    } catch { /* not url-encoded */ }
  }

  return null;
}

/** 递归推断对象结构（保留键名和类型，不保留具体值；语义关键字段保留值） */
const SEMANTIC_KEYS = new Set(['action', 'type', 'op', 'method', 'cmd', 'command', 'mode']);

function inferObjectShape(obj: Record<string, unknown>, depth = 0): Record<string, unknown> {
  if (depth > 3) return { _type: 'deep' };

  const shape: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(obj)) {
    if (SEMANTIC_KEYS.has(key.toLowerCase()) && typeof value === 'string') {
      // 语义关键字段保留实际值
      shape[key] = value;
    } else if (value === null) {
      shape[key] = 'null';
    } else if (Array.isArray(value)) {
      shape[key] = { _type: 'array', _length: value.length };
    } else if (typeof value === 'object') {
      shape[key] = inferObjectShape(value as Record<string, unknown>, depth + 1);
    } else {
      shape[key] = typeof value;
    }
  }
  return shape;
}

/** 推断参数值的类型 */
function inferParamType(value: string): string {
  if (/^\d+$/.test(value)) return 'number';
  if (value === 'true' || value === 'false') return 'boolean';
  if (/^\d+\.\d+$/.test(value)) return 'float';
  if (value === '') return 'empty';
  return 'string';
}
