/**
 * 采集事件总线 —— 解耦采集层与其他消费方
 *
 * 事件类型：
 * - request-captured: 一条完整的 CaptureRecord 已写入
 * - scenario-started: 场景开始执行
 * - scenario-ended: 场景执行结束
 * - session-expired: 检测到鉴权失败
 */
import { EventEmitter } from 'node:events';
import type { CaptureRecord } from '../model/capture-record.js';
import type { ScenarioResult } from '../model/scenario-model.js';

export interface CaptureEvents {
  'request-captured': [record: CaptureRecord];
  'scenario-started': [scenarioId: string];
  'scenario-ended': [result: ScenarioResult];
  'session-expired': [scenarioId: string, statusCode: number];
}

export class CaptureEventBus extends EventEmitter {
  override emit<K extends keyof CaptureEvents>(event: K, ...args: CaptureEvents[K]): boolean {
    return super.emit(event, ...args);
  }

  override on<K extends keyof CaptureEvents>(event: K, listener: (...args: CaptureEvents[K]) => void): this {
    return super.on(event, listener as (...args: unknown[]) => void);
  }
}
