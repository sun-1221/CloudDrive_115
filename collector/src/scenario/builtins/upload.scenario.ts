/**
 * 上传链路采集场景 —— 对应 requirements.md 需求 7 的"待补采集"标记
 *
 * 目标：触发 115 网页端的真实上传流程，让网络监听器记录完整请求链路
 *   - /upload/init 或等效初始化接口
 *   - /upload/get_token 或等效 OSS 凭证接口
 *   - OSS/callback 回传接口
 *
 * 破坏性：本场景会实际上传一个探测文件（≤ 20 bytes）到 115 根目录。
 * 采集完成后会尝试将其移至回收站，但不保证百分百清理成功。
 *
 * 触发方式：必须显式通过 `COLLECTOR_ENABLE_DESTRUCTIVE=1` 环境变量才会真正执行上传动作，
 * 否则仅观察页面交互元素是否可达并直接退出，保证默认 `yarn collect` 安全。
 */
import { writeFile, mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import type { ScenarioContext, ScenarioDefinition } from '../../model/scenario-model.js';
import { BaseScenario } from '../base-scenario.js';

const DESTRUCTIVE_ENV_FLAG = 'COLLECTOR_ENABLE_DESTRUCTIVE';

export class UploadScenario extends BaseScenario {
  readonly definition: ScenarioDefinition = {
    id: 'upload',
    description: '上传链路采集（init / OSS 凭证 / callback）— 破坏性场景',
    tags: ['destructive', 'upload'],
    entryUrl: '/?cid=0&offset=0&tab=&mode=wangpan',
    preconditions: {
      accountState: ['logged-in'],
      dataSamples: ['根目录可写'],
      requiredResources: ['storage-quota'],
    },
    requiredDataProbe: null,
    destructive: true,
    timeout: 60_000,
    exitCriteria: {
      minCapturedRequests: 1,
      finalUrlPattern: null,
      customCheck: null,
    },
  };

  protected async run(ctx: ScenarioContext): Promise<void> {
    const { page, config, logger } = ctx;
    const destructiveEnabled = process.env[DESTRUCTIVE_ENV_FLAG] === '1';

    if (!destructiveEnabled) {
      logger.warn(
        { env: DESTRUCTIVE_ENV_FLAG },
        '破坏性场景未启用，跳过实际上传（需设置 COLLECTOR_ENABLE_DESTRUCTIVE=1）',
      );
    }

    // 1. 进入文件管理页
    ctx.captureSession.setCurrentAction('navigate-file-manager');
    logger.info('导航到文件管理页');
    await page.goto(`${config.baseUrl}/?cid=0&offset=0&tab=&mode=wangpan`, {
      waitUntil: 'domcontentloaded',
    });
    try {
      await page.waitForResponse(
        (resp) => resp.url().includes('/files') && resp.status() === 200,
        { timeout: 15_000 },
      );
    } catch {
      logger.warn('文件列表 API 未检测到');
    }
    await page.waitForTimeout(config.pageStabilityMs * 2);

    if (!destructiveEnabled) {
      // 仅观察上传按钮是否存在，不实际上传
      ctx.captureSession.setCurrentAction('probe-upload-ui');
      const uploadSelectors = [
        'input[type="file"]',
        '[data-action="upload"]',
        'a:has-text("上传")',
        'button:has-text("上传")',
      ];
      for (const selector of uploadSelectors) {
        const count = await page.locator(selector).count();
        if (count > 0) {
          logger.info({ selector, count }, '检测到上传入口元素');
        }
      }
      logger.info('上传场景（只读探测模式）完成');
      return;
    }

    // 2. 准备探测文件（20 bytes 极小文本）
    ctx.captureSession.setCurrentAction('prepare-probe-file');
    const tempDir = await mkdtemp(path.join(tmpdir(), '115-collector-'));
    const probeName = `115-collector-probe-${Date.now()}.txt`;
    const probePath = path.join(tempDir, probeName);
    await writeFile(probePath, 'collector probe sample', 'utf-8');
    logger.info({ probePath }, '探测文件已生成');

    try {
      // 3. 双通路尝试触发上传
      //    通路 A：如果 input[type=file] 已存在且可以直接 setInputFiles，优先走此路径
      //    通路 B：否则订阅 filechooser 事件，通过点击"上传"按钮 + 二级菜单触发
      ctx.captureSession.setCurrentAction('trigger-upload-input');
      logger.info('准备触发上传');

      // 预先订阅 filechooser 事件（Playwright 要求在触发动作前订阅）
      const fileChooserPromise = page
        .waitForEvent('filechooser', { timeout: 15_000 })
        .catch(() => null);

      // 通路 A：直接尝试 setInputFiles（部分 SPA 已在 DOM 中预置 input[type=file]）
      const fileInput = page.locator('input[type="file"]').first();
      let uploadTriggered = false;
      if ((await fileInput.count()) > 0) {
        try {
          await fileInput.setInputFiles(probePath, { timeout: 3_000 });
          uploadTriggered = true;
          logger.info('通路 A：直接 setInputFiles 成功');
        } catch {
          logger.warn('通路 A 失败，切换到 filechooser 事件监听');
        }
      }

      // 通路 B：点击"上传"按钮 → 等待二级菜单 → 点击"上传文件"
      if (!uploadTriggered) {
        ctx.captureSession.setCurrentAction('click-upload-button');
        const uploadBtnSelectors = [
          '[data-action="upload"]',
          'a[menu="upload_btn"]',
          'a[menu="upload"]',
          '.upload-btn',
          'a:has-text("上传"):not(:has-text("上传至"))',
          'button:has-text("上传"):not(:has-text("上传至"))',
        ];
        let buttonClicked = false;
        for (const selector of uploadBtnSelectors) {
          const btn = page.locator(selector).first();
          if ((await btn.count()) > 0 && (await btn.isVisible().catch(() => false))) {
            try {
              await btn.click({ timeout: 3_000 });
              buttonClicked = true;
              logger.info({ selector }, '通路 B：已点击"上传"按钮');
              break;
            } catch {
              logger.warn({ selector }, '"上传"按钮点击失败，尝试下一个');
            }
          }
        }
        if (!buttonClicked) {
          logger.warn('未能点击"上传"按钮');
        } else {
          await page.waitForTimeout(config.actionDelayMs * 2);
          // 二级菜单：尝试点击"上传文件"（非"上传文件夹"）
          const menuItemSelectors = [
            'li[menu="upload_file"]',
            'a[menu="upload_file"]',
            'li:has-text("上传文件"):not(:has-text("上传文件夹"))',
            'a:has-text("上传文件"):not(:has-text("上传文件夹"))',
            '[data-menu="upload_file"]',
          ];
          for (const selector of menuItemSelectors) {
            const menuItem = page.locator(selector).first();
            if ((await menuItem.count()) > 0 && (await menuItem.isVisible().catch(() => false))) {
              try {
                await menuItem.click({ timeout: 3_000 });
                logger.info({ selector }, '已点击"上传文件"菜单项');
                break;
              } catch {
                logger.warn({ selector }, '"上传文件"菜单项点击失败，尝试下一个');
              }
            }
          }
        }

        // 等待 filechooser 事件 → 注入探测文件
        const chooser = await fileChooserPromise;
        if (chooser) {
          await chooser.setFiles(probePath);
          uploadTriggered = true;
          logger.info('通路 B：filechooser 已响应，文件已注入');
        } else {
          logger.warn('通路 B 亦失败：未在 15s 内捕获 filechooser 事件');
        }
      }

      if (!uploadTriggered) {
        logger.warn('两条通路均失败，上传未触发；仍保存已采集的页面交互流量');
        return;
      }

      // 4. 等待上传相关接口
      ctx.captureSession.setCurrentAction('wait-upload-apis');
      try {
        await page.waitForResponse(
          (resp) =>
            (resp.url().includes('upload') ||
              resp.url().includes('init') ||
              resp.url().includes('get_token') ||
              resp.url().includes('oss')) &&
            resp.status() < 500,
          { timeout: 30_000 },
        );
        logger.info('上传接口已命中');
      } catch {
        logger.warn('未在超时内检测到上传接口响应');
      }
      // 给 OSS / callback 留足时间完成多步请求
      await page.waitForTimeout(8_000);

      // 5. 采集后清理：尝试将探测文件移至回收站
      ctx.captureSession.setCurrentAction('cleanup-probe-file');
      logger.info('尝试清理上传的探测文件');
      await page.evaluate(
        `(async () => {
           try {
             const listResp = await fetch(
               'https://webapi.115.com/files?aid=1&cid=0&o=user_utime&asc=0&offset=0&limit=50&show_dir=0&format=json',
               { credentials: 'include' },
             ).then((r) => r.json());
             const target = (listResp?.data || []).find((f) =>
               (f?.n || f?.file_name || '').includes('115-collector-probe-'),
             );
             if (target) {
               const fid = target.fid || target.file_id;
               if (fid) {
                 const body = new URLSearchParams();
                 body.append('fid[0]', fid);
                 body.append('ignore_warn', '1');
                 await fetch('https://webapi.115.com/rb/delete', {
                   method: 'POST',
                   credentials: 'include',
                   headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                   body: body.toString(),
                 });
               }
             }
           } catch (e) { /* 忽略清理失败 */ }
         })()`,
      );
      await page.waitForTimeout(config.actionDelayMs);
    } finally {
      // 6. 清理本地临时文件
      await rm(tempDir, { recursive: true, force: true }).catch(() => {});
    }

    logger.info('上传场景采集完成');
  }
}
