/**
 * Analyzer —— 分析流程编排，对应设计文档第二阶段
 *
 * 从原始 NDJSON 记录读取 → 归一化 → 指纹 → 聚合 → 输出
 */
import { readFile, writeFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { CollectorConfig } from '../config/schema.js';
import type { CaptureRecord } from '../model/capture-record.js';
import type { EndpointModel } from '../model/endpoint-model.js';
import type { Logger } from 'pino';
import { PathResolver } from '../storage/path-resolver.js';
import { mergeRecordsToEndpoints } from '../parser/endpoint-merger.js';

export interface AnalyzerOptions {
  config: CollectorConfig;
  logger: Logger;
}

export interface AnalyzeResult {
  runId: string;
  totalRecords: number;
  uniqueEndpoints: number;
  highConfidence: number;
  lowConfidence: number;
  endpoints: EndpointModel[];
}

export class Analyzer {
  constructor(private readonly options: AnalyzerOptions) {}

  /** 分析指定 run 的原始记录 */
  async analyze(runId: string): Promise<AnalyzeResult> {
    const { config, logger } = this.options;
    const paths = new PathResolver(config.outputDir, runId);

    logger.info({ runId, rawDir: paths.rawDir }, '读取原始记录');

    // 1. 读取所有 NDJSON 文件
    const records = await this.loadRecords(paths.rawDir);
    logger.info({ count: records.length }, '原始记录已加载');

    if (records.length === 0) {
      logger.warn({ runId }, '没有找到原始记录');
      return {
        runId,
        totalRecords: 0,
        uniqueEndpoints: 0,
        highConfidence: 0,
        lowConfidence: 0,
        endpoints: [],
      };
    }

    // 2. 聚合为 EndpointModel
    const endpoints = mergeRecordsToEndpoints(records);
    logger.info({ count: endpoints.length }, '接口聚合完成');

    // 3. 统计
    const highConfidence = endpoints.filter((e) => e.confidence === 'high').length;
    const lowConfidence = endpoints.filter((e) => e.confidence === 'low').length;

    // 4. 写入归一化结果
    const endpointsPath = join(paths.normalizedDir, 'endpoints.json');
    await writeFile(endpointsPath, JSON.stringify(endpoints, null, 2), 'utf-8');
    logger.info({ path: endpointsPath }, '接口模型已写入');

    // 5. 写入统计摘要
    const summary = {
      runId,
      analyzedAt: new Date().toISOString(),
      totalRecords: records.length,
      uniqueEndpoints: endpoints.length,
      highConfidence,
      mediumConfidence: endpoints.filter((e) => e.confidence === 'medium').length,
      lowConfidence,
      byMethod: this.countByMethod(endpoints),
      byScenario: this.countByScenario(records),
      byStatus: this.countByStatus(records),
    };
    const summaryPath = join(paths.reportsDir, 'summary.json');
    await writeFile(summaryPath, JSON.stringify(summary, null, 2), 'utf-8');
    logger.info({ path: summaryPath }, '统计摘要已写入');

    // 6. 写入低置信度复核列表
    const lowConfEndpoints = endpoints.filter((e) => e.confidence === 'low');
    if (lowConfEndpoints.length > 0) {
      const reviewPath = join(paths.reportsDir, 'review-low-confidence.json');
      await writeFile(reviewPath, JSON.stringify(lowConfEndpoints, null, 2), 'utf-8');
      logger.info({ path: reviewPath, count: lowConfEndpoints.length }, '低置信度复核列表已写入');
    }

    // 7. 更新 manifest 中的 uniqueEndpoints 计数
    await this.updateManifest(paths, endpoints.length);

    return {
      runId,
      totalRecords: records.length,
      uniqueEndpoints: endpoints.length,
      highConfidence,
      lowConfidence,
      endpoints,
    };
  }

  /** 从 raw 目录加载所有 NDJSON 记录 */
  private async loadRecords(rawDir: string): Promise<CaptureRecord[]> {
    const records: CaptureRecord[] = [];

    let files: string[];
    try {
      files = await readdir(rawDir);
    } catch {
      return records;
    }

    const ndjsonFiles = files.filter((f) => f.endsWith('.ndjson'));

    for (const file of ndjsonFiles) {
      const content = await readFile(join(rawDir, file), 'utf-8');
      const lines = content.trim().split('\n').filter((l) => l.length > 0);

      for (const line of lines) {
        try {
          records.push(JSON.parse(line) as CaptureRecord);
        } catch {
          this.options.logger.warn({ file, line: line.slice(0, 100) }, '解析 NDJSON 行失败');
        }
      }
    }

    return records;
  }

  /** 按 HTTP 方法统计接口数 */
  private countByMethod(endpoints: EndpointModel[]): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const ep of endpoints) {
      counts[ep.method] = (counts[ep.method] ?? 0) + 1;
    }
    return counts;
  }

  /** 按场景统计记录数 */
  private countByScenario(records: CaptureRecord[]): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const r of records) {
      counts[r.scenarioId] = (counts[r.scenarioId] ?? 0) + 1;
    }
    return counts;
  }

  /** 按状态码统计记录数 */
  private countByStatus(records: CaptureRecord[]): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const r of records) {
      const key = r.status !== null ? String(r.status) : 'failed';
      counts[key] = (counts[key] ?? 0) + 1;
    }
    return counts;
  }

  /** 更新 manifest.json 中的 uniqueEndpoints 计数 */
  private async updateManifest(paths: PathResolver, uniqueEndpoints: number): Promise<void> {
    try {
      const content = await readFile(paths.manifestPath, 'utf-8');
      const manifest = JSON.parse(content);
      manifest.counters.uniqueEndpoints = uniqueEndpoints;
      await writeFile(paths.manifestPath, JSON.stringify(manifest, null, 2), 'utf-8');
    } catch {
      this.options.logger.warn('更新 manifest 失败');
    }
  }
}
