/**
 * 分享场景 —— 采集分享列表、分享详情等 API
 *
 * 115 网页端的分享管理功能，包括我的分享列表、分享链接详情等。
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class ShareScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'share',
    description: '分享列表、分享详情采集',
    tags: ['core', 'readonly'],
    entryUrl: '/?tab=share&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: [],
      requiredResources: [],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: 30_000,
    exitCriteria: {
      minCapturedRequests: 2,
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

    // 2. 尝试点击左侧导航的"我的分享"入口
    ctx.captureSession.setCurrentAction('navigate-share');
    logger.info('尝试进入分享管理页面');

    const shareNavSelectors = [
      'a[href*="tab=share"]',
      '[data-tab="share"]',
      '.left-nav a:has-text("分享")',
      '.aside-nav a:has-text("分享")',
      'a:has-text("我的分享")',
      'a:has-text("分享管理")',
    ];

    let navigated = false;
    for (const selector of shareNavSelectors) {
      const navItem = page.locator(selector).first();
      if (await navItem.count() > 0) {
        await navItem.click();
        navigated = true;
        logger.info({ selector }, '已点击分享导航');
        break;
      }
    }

    if (!navigated) {
      logger.info('未找到分享导航，通过 URL 导航');
      await page.goto(`${config.baseUrl}/?tab=share&mode=wangpan`, { waitUntil: 'domcontentloaded' });
    }

    // 等待分享相关 API
    try {
      await page.waitForResponse(
        (resp) => (
          resp.url().includes('share') ||
          resp.url().includes('usershare')
        ) && resp.status() === 200,
        { timeout: 10_000 },
      );
      logger.info('分享 API 已返回');
    } catch {
      logger.warn('分享 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 直接调用分享相关核心 API
    ctx.captureSession.setCurrentAction('api-direct-share');
    logger.info('直接调用分享核心 API');

    // 我的分享列表
    await page.evaluate(
      `fetch("https://webapi.115.com/share/slist?offset=0&limit=30&format=json", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 分享发送列表
    await page.evaluate(
      `fetch("https://webapi.115.com/share/send?offset=0&limit=30&format=json", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 分享接收列表
    await page.evaluate(
      `fetch("https://webapi.115.com/share/receive?offset=0&limit=30&format=json", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 4. 滚动查看更多
    ctx.captureSession.setCurrentAction('scroll-share-list');
    logger.info('滚动分享列表');
    for (let i = 0; i < 3; i++) {
      await page.evaluate(`window.scrollBy(0, window.innerHeight * 0.8)`);
      await page.waitForTimeout(500);
    }
    await page.waitForTimeout(config.pageStabilityMs);

    logger.info('分享场景采集完成');
  }
}
