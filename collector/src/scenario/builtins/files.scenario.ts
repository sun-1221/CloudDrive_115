/**
 * 文件列表场景 —— 采集目录切换、分页、排序、列表请求
 */
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

export class FilesScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'files',
    description: '文件列表、分页、切换目录',
    tags: ['core', 'readonly'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: ['至少存在一个文件或文件夹'],
      requiredResources: [],
    },
    requiredDataProbe: null,
    destructive: false,
    timeout: null,
    exitCriteria: {
      minCapturedRequests: 5,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;
    const fileManagerUrl = `${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`;

    // 1. 导航到文件管理页（115 的核心文件列表入口）
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    logger.info({ url: fileManagerUrl }, '导航到文件管理页');
    await page.goto(fileManagerUrl, { waitUntil: 'domcontentloaded' });

    // 2. 等待文件列表 API 返回
    ctx.captureSession.setCurrentAction('wait-file-list-api');
    logger.info('等待文件列表 API');
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.url().includes('cid=') && resp.status() === 200,
        { timeout: 15_000 },
      );
      logger.info('文件列表 API 已返回');
    } catch {
      logger.warn('未检测到文件列表 API，继续执行');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    // 3. 尝试进入第一个文件夹（触发目录切换 API）
    ctx.captureSession.setCurrentAction('enter-folder');
    logger.info('尝试进入第一个文件夹');

    // 115 文件列表中文件夹的选择器（多种可能）
    const folderSelectors = [
      'li[file_type="0"]',
      'li[data-type="folder"]',
      '.list-thumb li[file_type="0"]',
      '.file-list .folder-item',
    ];
    let folderClicked = false;
    for (const selector of folderSelectors) {
      const folder = page.locator(selector).first();
      if (await folder.count() > 0) {
        // 双击进入文件夹（115 使用双击）
        await folder.dblclick();
        folderClicked = true;
        logger.info({ selector }, '已双击进入文件夹');

        // 等待新目录的文件列表 API
        try {
          await page.waitForResponse(
            (resp) => resp.url().includes('/files') && resp.url().includes('cid=') && resp.status() === 200,
            { timeout: 10_000 },
          );
        } catch {
          logger.warn('目录切换后未检测到新的文件列表 API');
        }
        await page.waitForTimeout(config.pageStabilityMs);
        break;
      }
    }

    if (!folderClicked) {
      logger.warn('未找到文件夹，跳过目录切换');
    }

    // 4. 返回上级目录
    if (folderClicked) {
      ctx.captureSession.setCurrentAction('go-back');
      logger.info('返回上级目录');
      // 115 的返回上级按钮
      const backSelectors = ['.path-back', '.btn-back', '[data-action="back"]'];
      let wentBack = false;
      for (const selector of backSelectors) {
        const backBtn = page.locator(selector).first();
        if (await backBtn.count() > 0) {
          await backBtn.click();
          wentBack = true;
          break;
        }
      }
      if (!wentBack) {
        // 备选：用浏览器后退
        await page.goBack();
      }
      try {
        await page.waitForResponse(
          (resp) => resp.url().includes('/files') && resp.url().includes('cid=') && resp.status() === 200,
          { timeout: 10_000 },
        );
      } catch { /* ignore */ }
      await page.waitForTimeout(config.pageStabilityMs);
    }

    // 5. 切换文件列表排序方式（触发带不同参数的 API）
    ctx.captureSession.setCurrentAction('change-sort');
    logger.info('尝试切换排序方式');
    const sortSelectors = ['.sort-btn', '[data-action="sort"]', '.file-sort'];
    for (const selector of sortSelectors) {
      const sortBtn = page.locator(selector).first();
      if (await sortBtn.count() > 0) {
        await sortBtn.click();
        await page.waitForTimeout(config.actionDelayMs);
        // 选择第一个排序选项
        const sortOption = page.locator('.sort-menu li, .dropdown-menu li').first();
        if (await sortOption.count() > 0) {
          await sortOption.click();
          try {
            await page.waitForResponse(
              (resp) => resp.url().includes('/files') && resp.status() === 200,
              { timeout: 10_000 },
            );
          } catch { /* ignore */ }
          await page.waitForTimeout(config.pageStabilityMs);
        }
        break;
      }
    }

    // 6. 滚动列表触发分页加载
    ctx.captureSession.setCurrentAction('scroll-file-list');
    logger.info('滚动文件列表触发分页');
    for (let i = 0; i < 5; i++) {
      await page.evaluate(`
        const listContainer = document.querySelector('.list-contents') || document.querySelector('#js_data_list') || document.documentElement;
        listContainer.scrollTop += 600;
        window.scrollBy(0, window.innerHeight * 0.8);
      `);
      await page.waitForTimeout(800);
    }
    await page.waitForTimeout(config.pageStabilityMs);

    // 7. 尝试点击一个视频/文件来触发详情/预览 API
    ctx.captureSession.setCurrentAction('preview-file');
    logger.info('尝试触发文件预览');
    const fileSelectors = [
      'li[file_type="1"]',   // 文件类型
      'li[file_type="3"]',   // 视频文件
      'li[file_type="4"]',   // 音频文件
      '.list-thumb li:not([file_type="0"])',
    ];
    for (const selector of fileSelectors) {
      const file = page.locator(selector).first();
      if (await file.count() > 0) {
        await file.click();
        logger.info({ selector }, '已点击文件');
        await page.waitForTimeout(config.pageStabilityMs * 2);
        // 关闭预览（按 Esc）
        await page.keyboard.press('Escape');
        await page.waitForTimeout(config.actionDelayMs);
        break;
      }
    }

    logger.info('文件列表场景采集完成');
  }
}
