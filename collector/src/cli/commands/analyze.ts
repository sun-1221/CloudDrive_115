/**
 * analyze 命令 —— 对已有原始记录做归一化、聚合和统计
 */
import type { Command } from 'commander';
import { createLogger } from '../../logger/logger.js';
import { loadConfig } from '../../config/load-config.js';
import { Analyzer } from '../../app/analyzer.js';

export function registerAnalyzeCommand(program: Command): void {
  program
    .command('analyze')
    .description('对已有原始记录做归一化、接口聚合、去重和统计')
    .requiredOption('-r, --run <runIds...>', '要分析的 run ID（支持多个）')
    .option('-c, --config <path>', '配置文件路径')
    .action(async (opts) => {
      const config = await loadConfig({ configPath: opts.config });
      const logger = createLogger('analyze');
      const runIds: string[] = opts.run;

      logger.info({ runIds }, '开始分析');

      try {
        const analyzer = new Analyzer({ config, logger });

        for (const runId of runIds) {
          const result = await analyzer.analyze(runId);
          logger.info({
            runId,
            totalRecords: result.totalRecords,
            uniqueEndpoints: result.uniqueEndpoints,
            highConfidence: result.highConfidence,
            lowConfidence: result.lowConfidence,
          }, 'run 分析完成');
        }

        logger.info({ runCount: runIds.length }, '全部分析完成');
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        logger.error({ err: message }, '分析失败');
        process.exit(1);
      }
    });
}
