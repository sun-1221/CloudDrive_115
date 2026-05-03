/**
 * 云下载场景 —— 采集离线下载任务列表、配额信息等 API
 *
 * 对应开放平台 API：
 *   GET  /open/offline/get_task_list  — 获取用户云下载任务列表
 *   GET  /open/offline/get_quota_info — 获取云下载配额信息
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class OfflineScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'offline',
    description: '云下载任务列表、配额信息采集',
    tags: ['core', 'readonly'],
    entryUrl: '/?tab=offline&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: [],
      requiredResources: [],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: 30_000,
    exitCriteria: {
      minCapturedRequests: 3,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;

    // 1. 先进入文件管理 SPA
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    logger.info('导航到文件管理页');
    const fileManagerUrl = `${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`;
    await page.goto(fileManagerUrl, { waitUntil: 'domcontentloaded' });
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.status() === 200,
        { timeout: 15_000 },
      );
    } catch {
      logger.warn('文件列表 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 2. 尝试点击左侧导航的"云下载"入口
    ctx.captureSession.setCurrentAction('navigate-offline');
    logger.info('尝试进入云下载页面');

    const offlineNavSelectors = [
      'a[href*="tab=offline"]',
      '[data-tab="offline"]',
      '.left-nav a:has-text("云下载")',
      '.aside-nav a:has-text("云下载")',
      'a:has-text("离线下载")',
      'a:has-text("云下载")',
    ];

    let navigated = false;
    for (const selector of offlineNavSelectors) {
      const navItem = page.locator(selector).first();
      if (await navItem.count() > 0 && await navItem.isVisible()) {
        try {
          await navItem.click({ timeout: 5_000 });
          navigated = true;
          logger.info({ selector }, '已点击云下载导航');
          break;
        } catch {
          logger.warn({ selector }, '云下载导航点击失败，尝试下一个');
        }
      }
    }

    if (!navigated) {
      // 备选：直接通过 URL 参数导航
      logger.info('未找到云下载导航，通过 URL 导航');
      await page.goto(`${config.baseUrl}/?tab=offline&mode=wangpan`, { waitUntil: 'domcontentloaded' });
    }

    // 等待云下载相关 API
    try {
      await page.waitForResponse(
        (resp) => (
          resp.url().includes('offline') ||
          resp.url().includes('lixian') ||
          resp.url().includes('task')
        ) && resp.status() === 200,
        { timeout: 10_000 },
      );
      logger.info('云下载 API 已返回');
    } catch {
      logger.warn('云下载 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 直接通过 fetch 触发关键 API 确保覆盖
    ctx.captureSession.setCurrentAction('api-direct-offline');
    logger.info('直接调用云下载核心 API');

    // 获取云下载任务列表
    await page.evaluate(
      `fetch("https://115.com/web/lixian/?ct=lixian&ac=task_lists", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 获取云下载配额信息
    await page.evaluate(
      `fetch("https://115.com/web/lixian/?ct=lixian&ac=get_quota_package_info", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 4. 滚动查看更多任务
    ctx.captureSession.setCurrentAction('scroll-offline-list');
    logger.info('滚动云下载列表');
    for (let i = 0; i < 3; i++) {
      await page.evaluate(`window.scrollBy(0, window.innerHeight * 0.8)`);
      await page.waitForTimeout(500);
    }
    await page.waitForTimeout(config.pageStabilityMs);

    logger.info('云下载场景采集完成');
  }
}
