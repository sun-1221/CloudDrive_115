/**
 * 回收站场景 —— 采集回收站列表 API
 *
 * 对应开放平台 API：
 *   GET  /open/rb/list   — 回收站列表
 *   POST /open/rb/revert — 回收站还原（不执行，仅只读采集列表）
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class RecycleScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'recycle',
    description: '回收站列表采集',
    tags: ['core', 'readonly'],
    entryUrl: '/?tab=recycle&mode=wangpan',
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

    // 2. 尝试点击左侧导航的"回收站"入口
    ctx.captureSession.setCurrentAction('navigate-recycle');
    logger.info('尝试进入回收站页面');

    const recycleNavSelectors = [
      'a[href*="tab=recycle"]',
      '[data-tab="recycle"]',
      '.left-nav a:has-text("回收站")',
      '.aside-nav a:has-text("回收站")',
      'a:has-text("回收站")',
    ];

    let navigated = false;
    for (const selector of recycleNavSelectors) {
      const navItem = page.locator(selector).first();
      if (await navItem.count() > 0 && await navItem.isVisible()) {
        try {
          await navItem.click({ timeout: 5_000 });
          navigated = true;
          logger.info({ selector }, '已点击回收站导航');
          break;
        } catch {
          logger.warn({ selector }, '回收站导航点击失败，尝试下一个');
        }
      }
    }

    if (!navigated) {
      logger.info('未找到回收站导航，通过 URL 导航');
      await page.goto(`${config.baseUrl}/?tab=recycle&mode=wangpan`, { waitUntil: 'domcontentloaded' });
    }

    // 等待回收站 API
    try {
      await page.waitForResponse(
        (resp) => (
          resp.url().includes('rb') ||
          resp.url().includes('recycle') ||
          resp.url().includes('recyclebin')
        ) && resp.status() === 200,
        { timeout: 10_000 },
      );
      logger.info('回收站 API 已返回');
    } catch {
      logger.warn('回收站 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 直接调用回收站 API 确保覆盖
    ctx.captureSession.setCurrentAction('api-direct-recycle');
    logger.info('直接调用回收站核心 API');

    await page.evaluate(
      `fetch("https://webapi.115.com/rb?aid=7&cid=0&offset=0&limit=30&format=json", { credentials: "include" }).then(r => r.json()).catch(() => null)`,
    );
    await page.waitForTimeout(config.actionDelayMs);

    // 4. 滚动查看更多
    ctx.captureSession.setCurrentAction('scroll-recycle-list');
    logger.info('滚动回收站列表');
    for (let i = 0; i < 3; i++) {
      await page.evaluate(`window.scrollBy(0, window.innerHeight * 0.8)`);
      await page.waitForTimeout(500);
    }
    await page.waitForTimeout(config.pageStabilityMs);

    logger.info('回收站场景采集完成');
  }
}
