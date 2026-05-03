/**
 * HAR 输出 —— 利用 Playwright 内置 HAR 录制能力
 */
import type { BrowserContext } from 'playwright';
import { join } from 'node:path';

export interface HarWriterOptions {
  outputDir: string;
  scenarioId: string;
}

/**
 * 在 BrowserContext 创建时配置 HAR 录制路径
 * 返回路径供 manifest 记录
 */
export function getHarPath(options: HarWriterOptions): string {
  return join(options.outputDir, 'har', `${options.scenarioId}.har`);
}

/**
 * 获取创建 BrowserContext 时的 HAR 录制选项
 */
export function getHarRecordOptions(options: HarWriterOptions) {
  return {
    recordHar: {
      path: getHarPath(options),
      mode: 'minimal' as const,
    },
  };
}
