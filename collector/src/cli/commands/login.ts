/**
 * login 命令 —— 打开浏览器，人工完成登录并保存 storageState
 */
import type { Command } from 'commander';
import { loadConfig } from '../../config/load-config.js';
import { createLogger } from '../../logger/logger.js';
import { BrowserManager } from '../../browser/browser-manager.js';
import { SessionStore } from '../../browser/session-store.js';

export function registerLoginCommand(program: Command): void {
  program
    .command('login')
    .description('打开浏览器，人工完成登录并保存会话状态')
    .option('-c, --config <path>', '配置文件路径')
    .action(async (opts) => {
      const config = await loadConfig({ configPath: opts.config });
      const logger = createLogger('login');
      const browserManager = new BrowserManager(
        { ...config, headless: false },
        logger,
      );
      const sessionStore = new SessionStore(config.storageStatePath, logger);

      try {
        const browser = await browserManager.launch();
        const context = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
        const page = await context.newPage();

        logger.info({ baseUrl: config.baseUrl }, '浏览器已启动，请在页面中完成登录');
        await page.goto(config.baseUrl);

        // 等待用户手动登录 —— 轮询 context.cookies() 检测登录态
        // 115 的 UID/CID cookie 是 HttpOnly，document.cookie 读不到
        logger.info('等待登录完成... 检测到登录态后将自动保存');
        const deadline = Date.now() + 300_000; // 5 分钟超时
        let loggedIn = false;
        while (Date.now() < deadline) {
          const cookies = await context.cookies();
          const hasAuth = cookies.some(
            (c) => c.name === 'UID' || c.name === 'CID' || c.name === 'USSID',
          );
          if (hasAuth) {
            loggedIn = true;
            break;
          }
          await page.waitForTimeout(2_000);
        }
        if (!loggedIn) {
          throw new Error('timeout');
        }

        // 额外等待，确保所有 cookie 和 localStorage 写入完成
        await page.waitForTimeout(3_000);

        await sessionStore.save(context);
        logger.info({ path: config.storageStatePath }, '登录态已保存，可关闭浏览器');
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        if (message.includes('timeout')) {
          logger.error('登录超时（5 分钟），请重试');
        } else {
          logger.error({ err: message }, '登录过程出错');
        }
        process.exit(1);
      } finally {
        await browserManager.close();
      }
    });
}
