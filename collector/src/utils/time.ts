/**
 * 时间工具
 */

/** 等待指定毫秒数 */
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/** ISO 8601 时间戳 */
export function nowIso(): string {
  return new Date().toISOString();
}
