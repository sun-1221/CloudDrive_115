/**
 * 场景基类 —— 提供通用的执行框架和错误处理
 */
import type {
  ScenarioContext,
  ScenarioDefinition,
  ScenarioResult,
  ScenarioArtifacts,
} from '../model/scenario-model.js';
import type { IScenario } from './scenario.types.js';

export abstract class BaseScenario implements IScenario {
  abstract readonly definition: ScenarioDefinition;

  async execute(ctx: ScenarioContext): Promise<ScenarioResult> {
    const startedAt = new Date().toISOString();
    const start = Date.now();
    const artifacts: ScenarioArtifacts = {
      raw: null,
      har: null,
      screenshot: null,
      trace: null,
    };

    try {
      ctx.logger.info({ scenarioId: this.definition.id }, '场景开始');

      await this.run(ctx);

      const endedAt = new Date().toISOString();
      ctx.logger.info(
        { scenarioId: this.definition.id, captured: ctx.captureSession.getCapturedCount() },
        '场景完成',
      );

      return {
        scenarioId: this.definition.id,
        status: 'passed',
        reason: null,
        capturedCount: ctx.captureSession.getCapturedCount(),
        startedAt,
        endedAt,
        durationMs: Date.now() - start,
        artifactPaths: artifacts,
      };
    } catch (err) {
      const endedAt = new Date().toISOString();
      const message = err instanceof Error ? err.message : String(err);
      ctx.logger.error({ scenarioId: this.definition.id, err: message }, '场景失败');

      // 错误截图
      try {
        const screenshotPath = `screenshots/${this.definition.id}-error.png`;
        await ctx.page.screenshot({ path: screenshotPath, fullPage: true });
        artifacts.screenshot = screenshotPath;
      } catch {
        ctx.logger.warn('截图保存失败');
      }

      return {
        scenarioId: this.definition.id,
        status: 'failed',
        reason: message,
        capturedCount: ctx.captureSession.getCapturedCount(),
        startedAt,
        endedAt,
        durationMs: Date.now() - start,
        artifactPaths: artifacts,
      };
    }
  }

  /** 子类实现具体的场景采集逻辑 */
  protected abstract run(ctx: ScenarioContext): Promise<void>;
}
