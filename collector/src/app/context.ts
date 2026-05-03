/**
 * 应用上下文 —— 全局共享的运行时资源容器
 */
import type { CollectorConfig } from '../config/schema.js';
import type { Logger } from 'pino';
import type { CaptureEventBus } from '../capture/event-bus.js';

export interface AppContext {
  config: CollectorConfig;
  logger: Logger;
  eventBus: CaptureEventBus;
  runId: string;
}
