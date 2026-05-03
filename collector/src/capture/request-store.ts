/**
 * 请求暂存 —— 暂存进行中的请求，等待响应关联
 */
import type { CaptureRecord } from '../model/capture-record.js';

export class RequestStore {
  private readonly pending = new Map<string, Partial<CaptureRecord>>();

  set(networkId: string, partial: Partial<CaptureRecord>): void {
    this.pending.set(networkId, partial);
  }

  get(networkId: string): Partial<CaptureRecord> | undefined {
    return this.pending.get(networkId);
  }

  remove(networkId: string): Partial<CaptureRecord> | undefined {
    const record = this.pending.get(networkId);
    this.pending.delete(networkId);
    return record;
  }

  clear(): void {
    this.pending.clear();
  }
}
