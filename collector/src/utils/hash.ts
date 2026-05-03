/**
 * 哈希工具
 */
import { createHash } from 'node:crypto';

/** 计算内容的 SHA-256 前 16 位 */
export function shortHash(content: string | Buffer): string {
  return createHash('sha256').update(content).digest('hex').slice(0, 16);
}
