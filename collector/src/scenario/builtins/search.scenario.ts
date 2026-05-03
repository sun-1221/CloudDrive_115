/**
 * 搜索场景 —— 采集搜索请求与不同结果状态
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class SearchScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'search',
    description: '搜索请求、有结果/无结果分支',
    tags: ['core', 'readonly'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: [],
      requiredResources: [],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: null,
    exitCriteria: {
      minCapturedRequests: 2,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;
    const fileManagerUrl = `${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`;

    // 1. 先进入文件管理 SPA（搜索功能在文件管理页内）
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    logger.info('导航到文件管理页');
    await page.goto(fileManagerUrl, { waitUntil: 'domcontentloaded' });

    // 等待 SPA 加载完成
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.url().includes('cid=') && resp.status() === 200,
        { timeout: 15_000 },
      );
    } catch {
      logger.warn('文件列表 API 未检测到，继续执行搜索');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 2. 搜索常见关键词（有结果）
    ctx.captureSession.setCurrentAction('search-with-results');
    logger.info('执行搜索：常见关键词');
    await this.performSearch(page, config, logger, '视频');
    await page.waitForTimeout(config.pageStabilityMs);

    // 3. 返回文件列表
    ctx.captureSession.setCurrentAction('return-to-files');
    logger.info('返回文件列表');
    await page.goto(fileManagerUrl, { waitUntil: 'domcontentloaded' });
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.status() === 200,
        { timeout: 10_000 },
      );
    } catch { /* ignore */ }
    await page.waitForTimeout(config.pageStabilityMs);

    // 4. 搜索另一个关键词
    ctx.captureSession.setCurrentAction('search-another-keyword');
    logger.info('执行搜索：另一个关键词');
    await this.performSearch(page, config, logger, 'mp4');
    await page.waitForTimeout(config.pageStabilityMs);

    // 5. 搜索不存在的关键词（无结果分支）
    ctx.captureSession.setCurrentAction('search-no-results');
    logger.info('执行搜索：不存在的关键词');
    await this.performSearch(page, config, logger, `_nonexistent_${Date.now()}`);
    await page.waitForTimeout(config.pageStabilityMs);

    logger.info('搜索场景采集完成');
  }

  /** 在搜索框中输入关键词并等待搜索 API 返回 */
  private async performSearch(
    page: import('playwright').Page,
    config: import('../../config/schema.js').CollectorConfig,
    logger: import('pino').Logger,
    keyword: string,
  ): Promise<void> {
    // 115 的搜索框选择器（多种可能）
    const searchInputSelectors = [
      'input.search-input',
      '#js_search_input',
      'input[name="search_value"]',
      '.search-box input',
      'input[type="search"]',
      'input[placeholder*="搜索"]',
    ];

    let searched = false;
    for (const selector of searchInputSelectors) {
      const searchInput = page.locator(selector).first();
      if (await searchInput.count() > 0) {
        await searchInput.click();
        await searchInput.fill('');
        await searchInput.fill(keyword);
        logger.info({ selector, keyword }, '已输入搜索关键词');

        // 按回车触发搜索
        await page.keyboard.press('Enter');

        // 等待搜索 API 返回
        try {
          await page.waitForResponse(
            (resp) => (resp.url().includes('/files/search') || resp.url().includes('search_value')) && resp.status() === 200,
            { timeout: 10_000 },
          );
          logger.info({ keyword }, '搜索 API 已返回');
        } catch {
          logger.warn({ keyword }, '搜索 API 未检测到');
        }
        searched = true;
        break;
      }
    }

    if (!searched) {
      // 备选：通过 API URL 直接触发搜索
      logger.info({ keyword }, '未找到搜索框，通过 URL 触发搜索');
      const searchUrl = `https://webapi.115.com/files/search?offset=0&limit=40&search_value=${encodeURIComponent(keyword)}&aid=1&cid=0&format=json`;
      await page.evaluate(
        `fetch("${searchUrl}", { credentials: "include" }).then(r => r.json())`,
      );
      await page.waitForTimeout(config.actionDelayMs);
    }

    await page.waitForTimeout(config.actionDelayMs);
  }
}
