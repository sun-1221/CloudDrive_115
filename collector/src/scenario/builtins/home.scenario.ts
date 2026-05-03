/**
 * 首页初始化场景 —— 采集用户信息、初始化配置、首页加载接口
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class HomeScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'home',
    description: '首页初始化接口采集',
    tags: ['core', 'readonly'],
    entryUrl: '/',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: [],
      requiredResources: [],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: null,
    exitCriteria: {
      minCapturedRequests: 3,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;

    // 1. 导航到首页（登录后会自动进入文件管理 SPA）
    ctx.captureSession.setCurrentAction('navigate-home');
    logger.info('导航到首页');
    await page.goto(config.baseUrl, { waitUntil: 'domcontentloaded' });

    // 2. 等待 SPA 框架加载 + 文件列表 API 返回
    ctx.captureSession.setCurrentAction('wait-spa-init');
    logger.info('等待 SPA 初始化和文件列表 API');
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('webapi.115.com/files') && resp.status() === 200,
        { timeout: 15_000 },
      );
      logger.info('文件列表 API 已返回');
    } catch {
      logger.warn('未检测到文件列表 API，可能页面结构已变化，继续等待');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 等待用户信息 API
    ctx.captureSession.setCurrentAction('wait-user-info');
    logger.info('检查用户信息 API');
    await page.waitForTimeout(2000);

    // 4. 滚动页面触发懒加载请求
    ctx.captureSession.setCurrentAction('scroll-home');
    logger.info('滚动页面触发懒加载');
    await this.scrollPage(page);
    await page.waitForTimeout(config.actionDelayMs);

    // 5. 等待所有懒加载请求完成
    ctx.captureSession.setCurrentAction('wait-lazy-load');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(config.pageStabilityMs);

    logger.info('首页采集完成');
  }

  /** 逐步滚动页面以触发懒加载 */
  private async scrollPage(page: import('playwright').Page): Promise<void> {
    // 115 的文件列表在特定容器内，尝试滚动该容器
    const scrollSteps = 5;
    for (let i = 0; i < scrollSteps; i++) {
      await page.evaluate(`
        const listContainer = document.querySelector('.list-contents') || document.querySelector('#js_data_list') || document.documentElement;
        listContainer.scrollTop += 600;
        window.scrollBy(0, window.innerHeight * 0.8);
      `);
      await page.waitForTimeout(800);
    }
  }
}
