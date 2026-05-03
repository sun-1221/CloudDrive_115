/**
 * collect 命令 —— 执行场景并采集网络请求
 */
import type { Command } from 'commander';
import { loadConfig } from '../../config/load-config.js';
import { createLogger } from '../../logger/logger.js';
import { Runner } from '../../app/runner.js';

export function registerCollectCommand(program: Command): void {
  program
    .command('collect')
    .description('执行场景并采集网络请求，落盘原始记录')
    .option('-c, --config <path>', '配置文件路径')
    .option('-s, --scenario <ids...>', '指定场景 ID')
    .option('--analyze', '采集结束后自动进入分析流程', false)
    .option('--headless', '无头模式运行')
    .action(async (opts) => {
      const overrides: Record<string, unknown> = {};
      if (opts.scenario) overrides.scenarioIds = opts.scenario;
      if (opts.headless !== undefined) overrides.headless = opts.headless;

      const config = await loadConfig({ configPath: opts.config, overrides });
      const logger = createLogger('collect');

      try {
        const runner = new Runner({
          config,
          logger,
          autoAnalyze: opts.analyze,
        });

        const manifest = runner.run();
        const result = await manifest;

        logger.info({
          runId: result.runId,
          exitStatus: result.exitStatus,
          totalRequests: result.counters.totalRequests,
          passed: result.counters.passedScenarios,
          failed: result.counters.failedScenarios,
        }, '采集完成');

        if (result.exitStatus === 'error') {
          process.exit(1);
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        logger.error({ err: message }, '采集失败');
        process.exit(1);
      }
    });
}
