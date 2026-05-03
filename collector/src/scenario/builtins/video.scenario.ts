/**
 * 视频播放场景 —— 采集视频播放地址、字幕、进度等 API
 *
 * 对应开放平台 API：
 *   GET  /open/video/play     — 获取视频在线播放地址
 *   GET  /open/video/subtitle — 视频字幕列表
 *   GET  /open/video/history  — 获取视频播放进度
 *   POST /open/video/history  — 记忆视频播放进度
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class VideoScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'video',
    description: '视频播放地址、字幕、播放进度采集',
    tags: ['core', 'readonly'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: ['至少存在一个视频文件'],
      requiredResources: ['video'],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: 60_000,
    exitCriteria: {
      minCapturedRequests: 5,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;
    const fileManagerUrl = `${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`;

    // 1. 进入文件管理页
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    logger.info('导航到文件管理页');
    await page.goto(fileManagerUrl, { waitUntil: 'domcontentloaded' });
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.url().includes('cid=') && resp.status() === 200,
        { timeout: 15_000 },
      );
    } catch {
      logger.warn('文件列表 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 2. 通过筛选获取视频文件列表（type=4 为视频）
    ctx.captureSession.setCurrentAction('filter-video-files');
    logger.info('筛选视频文件');
    const videoFilterUrl = `${config.baseUrl}/?cid=0&offset=0&mode=wangpan&type=4`;
    await page.goto(videoFilterUrl, { waitUntil: 'domcontentloaded' });
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.status() === 200,
        { timeout: 15_000 },
      );
      logger.info('视频文件列表 API 已返回');
    } catch {
      logger.warn('视频文件列表 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 尝试点击第一个视频文件触发播放
    ctx.captureSession.setCurrentAction('play-video');
    logger.info('尝试点击视频文件触发播放');
    const videoSelectors = [
      'li[file_type="1"][is_video="1"]',
      'li[data-type="file"][data-ico="mp4"]',
      'li[data-type="file"][data-ico="mkv"]',
      'li[data-type="file"][data-ico="avi"]',
      'li .file-type-video',
      'li[is_video="1"]',
      // 通用：列表中第一个非文件夹项
      '.list-thumb li[file_type="1"]',
      '.list-contents li[file_type="1"]',
    ];

    let videoClicked = false;
    for (const selector of videoSelectors) {
      const videoItem = page.locator(selector).first();
      if (await videoItem.count() > 0) {
        await videoItem.dblclick();
        videoClicked = true;
        logger.info({ selector }, '已双击视频文件');
        break;
      }
    }

    if (videoClicked) {
      // 等待视频播放相关 API
      ctx.captureSession.setCurrentAction('wait-video-api');
      logger.info('等待视频播放 API');
      await page.waitForTimeout(config.pageStabilityMs * 3);

      // 等待可能的视频播放地址 API
      try {
        await page.waitForResponse(
          (resp) => (
            resp.url().includes('/video') ||
            resp.url().includes('play') ||
            resp.url().includes('m3u8')
          ) && resp.status() === 200,
          { timeout: 10_000 },
        );
        logger.info('视频播放 API 已返回');
      } catch {
        logger.warn('视频播放 API 未检测到');
      }
      await page.waitForTimeout(config.pageStabilityMs);

      // 关闭播放器
      ctx.captureSession.setCurrentAction('close-player');
      logger.info('关闭视频播放器');
      await page.keyboard.press('Escape');
      await page.waitForTimeout(config.actionDelayMs);
    } else {
      // 备选：直接通过 API 调用触发视频相关接口
      logger.warn('未找到视频文件，使用 API 直接触发');
      ctx.captureSession.setCurrentAction('api-fallback-video');

      // 先获取文件列表找到一个视频的 pick_code
      const pickCode = await page.evaluate(async () => {
        try {
          const resp = await fetch(
            'https://webapi.115.com/files?aid=1&cid=0&o=user_ptime&asc=0&offset=0&show_dir=0&limit=50&type=4&format=json',
            { credentials: 'include' },
          );
          const data = (await resp.json()) as { data?: { pc?: string; pick_code?: string }[] };
          if (data?.data && data.data.length > 0) {
            return data.data[0].pc || data.data[0].pick_code || null;
          }
          return null;
        } catch {
          return null;
        }
      });

      if (pickCode) {
        logger.info({ pickCode }, '通过 API 获取到视频 pick_code');

        // 触发视频播放地址 API（115 网页端真实视频接口）
        try {
          await page.evaluate(
            `fetch("https://webapi.115.com/files/video?pickcode=${pickCode}&share_id=0&local=1&format=json", { credentials: "include" }).then(r => r.json())`,
          );
          logger.info('视频播放地址 API 已触发');
        } catch {
          logger.warn('视频播放地址 API 调用失败');
        }
        await page.waitForTimeout(config.actionDelayMs);

        // 触发视频字幕列表 API
        try {
          await page.evaluate(
            `fetch("https://webapi.115.com/movies/subtitle?pickcode=${pickCode}&format=json", { credentials: "include" }).then(r => r.json())`,
          );
          logger.info('视频字幕 API 已触发');
        } catch {
          logger.warn('视频字幕 API 调用失败');
        }
        await page.waitForTimeout(config.actionDelayMs);
      } else {
        logger.warn('未找到任何视频文件');
      }
    }

    logger.info('视频播放场景采集完成');
  }
}
