/**
 * 生命周期管理 —— 处理进程信号和优雅关闭
 */
import type { Logger } from 'pino';

export class Lifecycle {
  private cleanupCallbacks: Array<() => Promise<void>> = [];

  constructor(private readonly logger: Logger) {}

  /** 注册清理回调 */
  onCleanup(callback: () => Promise<void>): void {
    this.cleanupCallbacks.push(callback);
  }

  /** 注册进程信号监听 */
  registerSignalHandlers(): void {
    const handler = async () => {
      this.logger.info('收到中止信号，开始清理...');
      await this.cleanup();
      process.exit(0);
    };

    process.on('SIGINT', handler);
    process.on('SIGTERM', handler);
  }

  /** 执行所有清理回调 */
  async cleanup(): Promise<void> {
    for (const cb of this.cleanupCallbacks) {
      try {
        await cb();
      } catch (err) {
        this.logger.error({ err }, '清理回调执行失败');
      }
    }
  }
}
