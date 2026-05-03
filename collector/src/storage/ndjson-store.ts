/**
 * NDJSON 存储 —— 原始记录的事实真相源
 */
import { createWriteStream, type WriteStream } from 'node:fs';
import { mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';

export class NdjsonStore {
  private stream: WriteStream | null = null;

  constructor(private readonly filePath: string) {}

  /** 打开文件流 */
  async open(): Promise<void> {
    await mkdir(dirname(this.filePath), { recursive: true });
    this.stream = createWriteStream(this.filePath, { flags: 'a', encoding: 'utf-8' });
  }

  /** 追加一条记录 */
  write(record: Record<string, unknown>): void {
    if (!this.stream) throw new Error('NdjsonStore 尚未打开');
    this.stream.write(JSON.stringify(record) + '\n');
  }

  /** 关闭文件流 */
  async close(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (!this.stream) { resolve(); return; }
      this.stream.end(() => {
        this.stream = null;
        resolve();
      });
      this.stream.on('error', reject);
    });
  }
}
