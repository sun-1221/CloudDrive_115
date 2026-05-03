/**
 * 视频播放进度读写采集场景 —— 对应 requirements.md 需求 18 的"待补采集"标记
 *
 * 目标：触发 115 网页端视频播放历史接口的读/写对样本
 *   - 写入：通过播放视频几秒后触发后端记忆
 *   - 读取：通过重新进入视频或直接调用获取接口
 *
 * 破坏性：写入播放进度是持久化副作用，但不会损坏用户数据。
 *
 * 实现策略：优先 API 直接调用，UI 操作作为备选。
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class VideoHistoryScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'video-history',
    description: '视频播放进度读写采集（写入 + 读取 配对样本）',
    tags: ['video', 'history'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: ['至少存在一个视频文件'],
      requiredResources: ['video'],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: 45_000,
    exitCriteria: {
      minCapturedRequests: 2,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;

    // 1. 进入文件管理页
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    await page.goto(`${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`, {
      waitUntil: 'domcontentloaded',
    });
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 2. 获取一个视频文件的 pick_code
    ctx.captureSession.setCurrentAction('probe-video-pickcode');
    const pickCode = await page.evaluate(async () => {
      try {
        const resp = await fetch(
          'https://webapi.115.com/files?aid=1&cid=0&o=user_ptime&asc=0&offset=0&show_dir=0&limit=50&type=4&format=json',
          { credentials: 'include' },
        );
        const data = (await resp.json()) as { data?: Array<{ pc?: string; pick_code?: string }> };
        if (data?.data && data.data.length > 0) {
          return data.data[0].pc || data.data[0].pick_code || null;
        }
        return null;
      } catch {
        return null;
      }
    });

    if (!pickCode) {
      logger.warn('未找到视频文件，播放进度场景无法继续');
      return;
    }
    logger.info({ pickCode }, '已获取视频 pick_code');

    // 3. 尝试多个候选写入接口（播放进度记忆）
    ctx.captureSession.setCurrentAction('write-play-history');
    logger.info('尝试写入播放进度');

    const writeCandidates = [
      // 候选：files/history POST
      {
        url: 'https://webapi.115.com/files/history',
        body: `pick_code=${pickCode}&op=update&time=30&definition=0`,
      },
      // 候选：movies/history POST
      {
        url: 'https://webapi.115.com/movies/history',
        body: `pick_code=${pickCode}&time=30`,
      },
      // 候选：video/history POST
      {
        url: 'https://webapi.115.com/video/history',
        body: `pick_code=${pickCode}&time=30`,
      },
    ];

    for (const { url, body } of writeCandidates) {
      try {
        await page.evaluate(
          ({ u, b }) =>
            fetch(u, {
              method: 'POST',
              credentials: 'include',
              headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
              body: b,
            })
              .then((r) => r.json())
              .catch(() => null),
          { u: url, b: body },
        );
        logger.info({ url }, '播放进度写入候选路径已调用');
      } catch (err) {
        logger.warn({ url, err: String(err) }, '播放进度写入调用失败');
      }
      await page.waitForTimeout(config.actionDelayMs);
    }

    // 4. 尝试多个候选读取接口
    ctx.captureSession.setCurrentAction('read-play-history');
    logger.info('尝试读取播放进度');

    const readCandidates = [
      `https://webapi.115.com/files/history?pick_code=${pickCode}&format=json`,
      `https://webapi.115.com/movies/history?pick_code=${pickCode}&format=json`,
      `https://webapi.115.com/video/history?pick_code=${pickCode}&format=json`,
      // 带 fetch 参数
      `https://webapi.115.com/files/history?pick_code=${pickCode}&fetch=one&format=json`,
    ];

    for (const url of readCandidates) {
      try {
        await page.evaluate(
          (u) => fetch(u, { credentials: 'include' }).then((r) => r.json()).catch(() => null),
          url,
        );
        logger.info({ url }, '播放进度读取候选路径已调用');
      } catch (err) {
        logger.warn({ url, err: String(err) }, '播放进度读取调用失败');
      }
      await page.waitForTimeout(config.actionDelayMs);
    }

    logger.info('播放进度场景采集完成');
  }
}
