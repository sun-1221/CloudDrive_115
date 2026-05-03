/**
 * Runner —— 采集主流程编排，对应设计文档 7.2 + 9.2
 */
import { writeFile, mkdir } from 'node:fs/promises';
import type { CollectorConfig } from '../config/schema.js';
import type { RunManifest, RunExitStatus } from '../model/report-model.js';
import type { ScenarioResult } from '../model/scenario-model.js';
import type { Logger } from 'pino';
import { BrowserManager } from '../browser/browser-manager.js';
import { ContextManager } from '../browser/context-manager.js';
import { PageManager } from '../browser/page-manager.js';
import { SessionStore } from '../browser/session-store.js';
import { NetworkListener } from '../capture/network-listener.js';
import { NdjsonStore } from '../storage/ndjson-store.js';
import { PathResolver } from '../storage/path-resolver.js';
import { ScenarioRegistry } from '../scenario/scenario-registry.js';
import { HomeScenario } from '../scenario/builtins/home.scenario.js';
import { FilesScenario } from '../scenario/builtins/files.scenario.js';
import { SearchScenario } from '../scenario/builtins/search.scenario.js';
import { VideoScenario } from '../scenario/builtins/video.scenario.js';
import { OfflineScenario } from '../scenario/builtins/offline.scenario.js';
import { RecycleScenario } from '../scenario/builtins/recycle.scenario.js';
import { ShareScenario } from '../scenario/builtins/share.scenario.js';
import { UploadScenario } from '../scenario/builtins/upload.scenario.js';
import { TranscodeScenario } from '../scenario/builtins/transcode.scenario.js';
import { VideoHistoryScenario } from '../scenario/builtins/video-history.scenario.js';
import { Lifecycle } from './lifecycle.js';
import { sleep } from '../utils/time.js';

export interface RunOptions {
  config: CollectorConfig;
  logger: Logger;
  autoAnalyze?: boolean;
}

export class Runner {
  private readonly lifecycle: Lifecycle;

  constructor(private readonly options: RunOptions) {
    this.lifecycle = new Lifecycle(options.logger);
  }

  /**
   * 执行完整的采集流程
   *
   * 1. 生成 runId
   * 2. 初始化 manifest 与目录结构
   * 3. 启动浏览器并校验登录态
   * 4. 逐个场景执行采集
   * 5. 写入 manifest 并关闭浏览器
   */
  async run(): Promise<RunManifest> {
    const { config, logger } = this.options;
    const runId = this.generateRunId();
    const paths = new PathResolver(config.outputDir, runId);
    const startedAt = new Date().toISOString();

    logger.info({ runId, outputDir: paths.runDir }, '采集开始');

    // 创建 run 目录结构
    await this.ensureDirectories(paths);

    // 校验登录态
    const sessionStore = new SessionStore(config.storageStatePath, logger);
    if (!sessionStore.exists()) {
      logger.error({ path: config.storageStatePath }, '登录态文件不存在，请先执行 login 命令');
      throw new Error('登录态文件不存在');
    }

    // 注册场景
    const registry = this.buildRegistry();
    const scenarios = registry.getByIds(config.scenarioIds);
    logger.info({ count: scenarios.length, ids: scenarios.map((s) => s.definition.id) }, '待执行场景');

    // 启动浏览器
    const browserManager = new BrowserManager(config, logger);
    const contextManager = new ContextManager(config, logger);
    const pageManager = new PageManager(config);
    const browser = await browserManager.launch();
    const browserVersion = browser.version();

    this.lifecycle.registerSignalHandlers();
    this.lifecycle.onCleanup(async () => browserManager.close());

    const scenarioResults: ScenarioResult[] = [];
    let totalCaptured = 0;
    let exitStatus: RunExitStatus = 'completed';

    // 逐个场景串行执行
    for (const scenario of scenarios) {
      const sid = scenario.definition.id;
      logger.info({ scenarioId: sid }, '开始执行场景');

      // 为每个场景创建独立的 NDJSON 存储
      const ndjsonPath = paths.scenarioRawPath(sid);
      const store = new NdjsonStore(ndjsonPath);
      await store.open();

      // 创建场景级 BrowserContext 和 Page
      let context;
      try {
        context = await contextManager.createScenarioContext(browser, {
          runId,
          scenarioId: sid,
          outputDir: paths.runDir,
        });
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        logger.error({ scenarioId: sid, err: msg }, '创建场景上下文失败');
        scenarioResults.push({
          scenarioId: sid,
          status: 'failed',
          reason: msg,
          capturedCount: 0,
          startedAt: new Date().toISOString(),
          endedAt: new Date().toISOString(),
          durationMs: 0,
          artifactPaths: { raw: ndjsonPath, har: null, screenshot: null, trace: null },
        });
        await store.close();
        continue;
      }

      const page = await pageManager.createPage(context);

      // 创建网络监听器
      const listener = new NetworkListener({
        runId,
        scenarioId: sid,
        allowedDomains: config.allowedDomains,
        captureResponseBody: config.captureResponseBody,
        maxBodySize: config.maxBodySize,
        logger: logger.child({ scenario: sid }),
        store,
      });
      listener.attach(page);

      // 执行场景
      const result = await scenario.execute({
        runId,
        scenarioId: sid,
        page,
        browserContext: context,
        config,
        logger: logger.child({ scenario: sid }),
        captureSession: listener,
      });

      // 记录 NDJSON 路径
      result.artifactPaths.raw = ndjsonPath;
      scenarioResults.push(result);
      totalCaptured += result.capturedCount;

      // 关闭场景资源
      await store.close();
      await contextManager.closeContext(context);

      logger.info(
        { scenarioId: sid, status: result.status, captured: result.capturedCount },
        '场景执行完毕',
      );

      // 检测鉴权失败（状态码 401/403）—— run 级致命错误
      if (result.status === 'failed' && result.reason?.includes('net::ERR')) {
        exitStatus = 'error';
        break;
      }

      // 场景间等待
      if (config.scenarioIntervalMs > 0) {
        await sleep(config.scenarioIntervalMs);
      }
    }

    await browserManager.close();

    // 统计
    const passed = scenarioResults.filter((r) => r.status === 'passed').length;
    const failed = scenarioResults.filter((r) => r.status === 'failed').length;
    const skipped = scenarioResults.filter((r) => r.status === 'skipped').length;

    if (exitStatus === 'completed' && failed > 0 && passed > 0) {
      exitStatus = 'partial';
    } else if (exitStatus === 'completed' && failed > 0 && passed === 0) {
      exitStatus = 'error';
    }

    const manifest: RunManifest = {
      runId,
      targetName: '115',
      baseUrl: config.baseUrl,
      configSnapshot: config as unknown as Record<string, unknown>,
      browserVersion,
      startedAt,
      endedAt: new Date().toISOString(),
      scenarioResults,
      counters: {
        totalRequests: totalCaptured,
        targetDomainRequests: totalCaptured,
        uniqueEndpoints: null,
        passedScenarios: passed,
        failedScenarios: failed,
        skippedScenarios: skipped,
      },
      artifactPaths: {
        raw: paths.rawDir,
        normalized: paths.normalizedDir,
        reports: paths.reportsDir,
        har: paths.harDir,
        screenshots: paths.screenshotsDir,
        traces: paths.tracesDir,
      },
      exitStatus,
    };

    // 写入 manifest.json
    await writeFile(paths.manifestPath, JSON.stringify(manifest, null, 2), 'utf-8');
    logger.info({ runId, manifestPath: paths.manifestPath, exitStatus }, '采集完成');

    return manifest;
  }

  /** 构建内置场景注册表
   *
   * 默认注册 9 个场景（7 个只读 + 2 个轻量副作用）。
   * 破坏性场景（upload 等）必须通过环境变量 `COLLECTOR_ENABLE_DESTRUCTIVE=1` 启用。
   */
  private buildRegistry(): ScenarioRegistry {
    const registry = new ScenarioRegistry();
    // 原有 7 个只读场景
    registry.register(new HomeScenario());
    registry.register(new FilesScenario());
    registry.register(new SearchScenario());
    registry.register(new VideoScenario());
    registry.register(new OfflineScenario());
    registry.register(new RecycleScenario());
    registry.register(new ShareScenario());
    // 新增 2 个轻量副作用场景（转码提交仅触发后端任务；播放进度记忆持久化但不损坏数据）
    registry.register(new TranscodeScenario());
    registry.register(new VideoHistoryScenario());
    // 破坏性场景：仅在显式启用时注册，避免默认 yarn collect 误伤
    if (process.env.COLLECTOR_ENABLE_DESTRUCTIVE === '1') {
      registry.register(new UploadScenario());
      this.options.logger.warn('已启用破坏性场景：upload');
    }
    return registry;
  }

  /** 创建 run 所需的目录结构 */
  private async ensureDirectories(paths: PathResolver): Promise<void> {
    await mkdir(paths.rawDir, { recursive: true });
    await mkdir(paths.normalizedDir, { recursive: true });
    await mkdir(paths.reportsDir, { recursive: true });
    await mkdir(paths.harDir, { recursive: true });
    await mkdir(paths.screenshotsDir, { recursive: true });
    await mkdir(paths.tracesDir, { recursive: true });
  }

  private generateRunId(): string {
    const now = new Date();
    const ts = now.toISOString().replace(/[:\-T]/g, '').slice(0, 14);
    const rand = Math.random().toString(36).slice(2, 8);
    return `${ts}-${rand}`;
  }
}
