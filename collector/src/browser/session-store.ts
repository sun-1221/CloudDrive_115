/**
 * 会话状态读写 —— storageState 的加载与保存
 */
import { existsSync } from 'node:fs';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { BrowserContext } from 'playwright';
import type { Logger } from 'pino';

export class SessionStore {
  constructor(
    private readonly path: string,
    private readonly logger: Logger,
  ) {}

  /** 检查登录态文件是否存在 */
  exists(): boolean {
    return existsSync(this.path);
  }

  /** 从 BrowserContext 保存登录态 */
  async save(context: BrowserContext): Promise<void> {
    await mkdir(dirname(this.path), { recursive: true });
    const state = await context.storageState();
    await writeFile(this.path, JSON.stringify(state, null, 2), 'utf-8');
    this.logger.info({ path: this.path }, '登录态已保存');
  }

  /** 读取已保存的登录态 JSON */
  async load(): Promise<string> {
    const content = await readFile(this.path, 'utf-8');
    return content;
  }
}
