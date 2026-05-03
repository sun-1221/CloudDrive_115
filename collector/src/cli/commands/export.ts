/**
 * export 命令 —— 导出 JSON / CSV / Markdown / OpenAPI 草稿
 */
import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { Command } from 'commander';
import { loadConfig } from '../../config/load-config.js';
import { createLogger } from '../../logger/logger.js';
import { PathResolver } from '../../storage/path-resolver.js';
import { exportJson } from '../../exporter/json-exporter.js';
import { exportCsv } from '../../exporter/csv-exporter.js';
import { exportMarkdown, exportCoverageReport } from '../../exporter/markdown-exporter.js';
import { exportOpenApiDraft } from '../../exporter/openapi-exporter.js';
import type { EndpointModel } from '../../model/endpoint-model.js';

export function registerExportCommand(program: Command): void {
  program
    .command('export')
    .description('导出接口台账和报告')
    .requiredOption('-r, --run <runId>', '要导出的 run ID')
    .option('-c, --config <path>', '配置文件路径')
    .option('-f, --format <formats...>', '导出格式', ['json', 'csv'])
    .action(async (opts) => {
      const config = await loadConfig({ configPath: opts.config });
      const logger = createLogger('export');
      const runId: string = opts.run;
      const formats: string[] = opts.format;

      logger.info({ runId, formats }, '开始导出');

      const paths = new PathResolver(config.outputDir, runId);
      const endpointsPath = join(paths.normalizedDir, 'endpoints.json');

      // 读取已分析的接口模型
      let endpoints: EndpointModel[];
      try {
        const content = await readFile(endpointsPath, 'utf-8');
        endpoints = JSON.parse(content) as EndpointModel[];
      } catch {
        logger.error({ path: endpointsPath }, '未找到接口模型，请先执行 analyze 命令');
        process.exit(1);
      }

      logger.info({ count: endpoints.length }, '已加载接口模型');

      try {
        for (const format of formats) {
          switch (format) {
            case 'json': {
              const outPath = join(paths.reportsDir, 'endpoints.json');
              await exportJson(endpoints, outPath);
              logger.info({ path: outPath }, 'JSON 导出完成');
              break;
            }
            case 'csv': {
              const outPath = join(paths.reportsDir, 'endpoints.csv');
              await exportCsv(endpoints, outPath);
              logger.info({ path: outPath }, 'CSV 导出完成');
              break;
            }
            case 'markdown': {
              const outPath = join(paths.reportsDir, 'endpoints.md');
              await exportMarkdown(endpoints, outPath);
              logger.info({ path: outPath }, 'Markdown 导出完成');
              break;
            }
            case 'openapi': {
              const outPath = join(paths.reportsDir, 'openapi-draft.json');
              await exportOpenApiDraft(endpoints, outPath);
              logger.info({ path: outPath }, 'OpenAPI 草稿导出完成');
              break;
            }
            case 'coverage': {
              const outPath = join(paths.reportsDir, 'coverage.md');
              await exportCoverageReport(endpoints, outPath);
              logger.info({ path: outPath }, '场景覆盖报告导出完成');
              break;
            }
            default:
              logger.warn({ format }, '不支持的导出格式，跳过');
          }
        }

        logger.info('导出完成');
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        logger.error({ err: message }, '导出失败');
        process.exit(1);
      }
    });
}
