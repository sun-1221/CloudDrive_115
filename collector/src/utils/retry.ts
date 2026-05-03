/**
 * 重试工具 —— 带退避策略的重试包装器
 */
import type { Logger } from 'pino';
import { sleep } from './time.js';

export interface RetryOptions {
  maxRetries: number;
  backoffMs: number;
  logger?: Logger;
}

/** 带退避重试的函数执行器 */
export async function withRetry<T>(
  fn: () => Promise<T>,
  options: RetryOptions,
): Promise<T> {
  let lastError: Error | undefined;

  for (let attempt = 0; attempt <= options.maxRetries; attempt++) {
    try {
      return await fn();
    } catch (err) {
      lastError = err instanceof Error ? err : new Error(String(err));

      if (attempt < options.maxRetries) {
        const delay = options.backoffMs * Math.pow(2, attempt);
        options.logger?.warn({ attempt, delay, error: lastError.message }, '重试中');
        await sleep(delay);
      }
    }
  }

  throw lastError;
}
