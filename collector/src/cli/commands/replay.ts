/**
 * replay 命令 —— 复用上一次 run 的配置重新执行采集
 */
import { readFile } from 'node:fs/promises';
import type { Command } from 'commander';
import { createLogger } from '../../logger/logger.js';
import { PathResolver } from '../../storage/path-resolver.js';
import { loadConfig } from '../../config/load-config.js';
import { Runner } from '../../app/runner.js';
import type { RunManifest } from '../../model/report-model.js';
import type { CollectorConfig } from '../../config/schema.js';

export function registerReplayCommand(program: Command): void {
  program
    .command('replay')
    .description('基于已有登录态重新执行指定场景，产生新的 runId')
    .requiredOption('-r, --run <runId>', '复用的 run ID')
    .option('-c, --config <path>', '配置文件路径覆盖')
    .option('-s, --scenario <ids...>', '覆盖场景列表')
    .action(async (opts) => {
      const logger = createLogger('replay');
      const sourceRunId: string = opts.run;

      logger.info({ sourceRunId }, '开始重放采集');

      // 读取源 run 的 manifest，提取 configSnapshot
      const baseConfig = await loadConfig({ configPath: opts.config });
      const paths = new PathResolver(baseConfig.outputDir, sourceRunId);

      let sourceConfig: Partial<CollectorConfig> = {};
      try {
        const manifestRaw = await readFile(paths.manifestPath, 'utf-8');
        const manifest = JSON.parse(manifestRaw) as RunManifest;
        sourceConfig = manifest.configSnapshot as Partial<CollectorConfig>;
        logger.info({ sourceRunId, scenarios: sourceConfig.scenarioIds }, '已加载源 run 配置');
      } catch {
        logger.warn({ sourceRunId }, '未找到源 manifest，使用默认配置');
      }

      // 覆盖参数优先级：CLI opts > 源 run 配置 > 默认配置
      const overrides: Record<string, unknown> = { ...sourceConfig };
      if (opts.scenario) overrides.scenarioIds = opts.scenario;

      const config = await loadConfig({ configPath: opts.config, overrides });

      try {
        const runner = new Runner({ config, logger });
        const result = await runner.run();

        logger.info({
          sourceRunId,
          newRunId: result.runId,
          exitStatus: result.exitStatus,
          totalRequests: result.counters.totalRequests,
          passed: result.counters.passedScenarios,
          failed: result.counters.failedScenarios,
        }, '重放采集完成');

        if (result.exitStatus === 'error') {
          process.exit(1);
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        logger.error({ err: message }, '重放采集失败');
        process.exit(1);
      }
    });
}
