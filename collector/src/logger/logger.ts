/**
 * 日志工厂 —— 基于 pino
 *
 * 不使用 pino/file transport（worker thread），
 * 直接写 stdout 以确保 Windows PowerShell 7 下中文正常显示。
 */
import pino, { type Logger } from 'pino';

export function createLogger(name: string): Logger {
  return pino({
    name,
    level: process.env.LOG_LEVEL ?? 'info',
  });
}
