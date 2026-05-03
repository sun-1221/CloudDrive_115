/**
 * 视频转码提交采集场景 —— 对应 requirements.md 需求 15 的"待补采集"标记
 *
 * 目标：触发 115 网页端视频转码提交接口，采集请求/响应样本
 *   - /files/video 播放探测（可能返回未转码状态）
 *   - 转码提交接口（路径待采集，可能为 /movies/transcode / vip_push / pay_push）
 *
 * 破坏性：提交转码任务不会修改原文件，但可能占用账号 VIP 配额。
 * 非 VIP 账号会返回明确错误响应，错误响应对 ErrorFieldAdapter 验证有价值。
 *
 * 实现策略：通过 API 直接调用，不依赖 UI 点击。
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class TranscodeScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'transcode',
    description: '视频转码提交采集 —— 需 VIP 权限才能成功，否则采集错误响应样本',
    tags: ['video', 'transcode'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: ['至少存在一个未转码视频文件'],
      requiredResources: ['video'],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: 45_000,
    exitCriteria: {
      minCapturedRequests: 1,
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

    // 2. 通过 API 获取一个视频文件的 pick_code
    ctx.captureSession.setCurrentAction('probe-video-pickcode');
    logger.info('查找视频文件 pick_code');
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
      logger.warn('未找到视频文件，转码场景无法继续');
      return;
    }
    logger.info({ pickCode }, '已获取视频 pick_code');

    // 3. 先触发播放探测，验证视频是否已转码
    ctx.captureSession.setCurrentAction('probe-video-play');
    await page.evaluate(
      `fetch("https://webapi.115.com/files/video?pickcode=${pickCode}&share_id=0&local=1&format=json", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs * 2);

    // 4. 尝试多个候选转码提交接口路径（采集请求与错误响应样本）
    ctx.captureSession.setCurrentAction('submit-transcode');
    logger.info('尝试多个转码提交接口路径');

    const transcodeCandidates = [
      // 候选路径 1：vip 推送转码
      `https://webapi.115.com/files/video_push?pickcode=${pickCode}&op=vip_push&format=json`,
      // 候选路径 2：pay 推送转码
      `https://webapi.115.com/files/video_push?pickcode=${pickCode}&op=pay_push&format=json`,
      // 候选路径 3：文档中提及的 movies/transcode
      `https://webapi.115.com/movies/transcode?pickcode=${pickCode}&format=json`,
      // 候选路径 4：video/push
      `https://webapi.115.com/video/push?pickcode=${pickCode}&op=vip_push&format=json`,
    ];

    for (const url of transcodeCandidates) {
      try {
        await page.evaluate(
          (u) => fetch(u, { credentials: 'include' }).then((r) => r.json()).catch(() => null),
          url,
        );
        logger.info({ url }, '转码候选路径已调用');
      } catch (err) {
        logger.warn({ url, err: String(err) }, '转码候选路径调用失败');
      }
      await page.waitForTimeout(config.actionDelayMs);
    }

    logger.info('转码场景采集完成（错误响应样本亦有价值）');
  }
}
