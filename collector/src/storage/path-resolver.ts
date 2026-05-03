/**
 * 路径解析 —— 统一管理 run 目录结构
 */
import { join } from 'node:path';

export class PathResolver {
  constructor(
    private readonly outputDir: string,
    private readonly runId: string,
  ) {}

  /** run 根目录 */
  get runDir(): string {
    return join(this.outputDir, this.runId);
  }

  /** 原始数据目录 */
  get rawDir(): string {
    return join(this.runDir, 'raw');
  }

  /** 归一化结果目录 */
  get normalizedDir(): string {
    return join(this.runDir, 'normalized');
  }

  /** 报表目录 */
  get reportsDir(): string {
    return join(this.runDir, 'reports');
  }

  /** HAR 目录 */
  get harDir(): string {
    return join(this.runDir, 'har');
  }

  /** 截图目录 */
  get screenshotsDir(): string {
    return join(this.runDir, 'screenshots');
  }

  /** trace 目录 */
  get tracesDir(): string {
    return join(this.runDir, 'traces');
  }

  /** 场景 NDJSON 文件路径 */
  scenarioRawPath(scenarioId: string): string {
    return join(this.rawDir, `${scenarioId}.ndjson`);
  }

  /** manifest.json 路径 */
  get manifestPath(): string {
    return join(this.runDir, 'manifest.json');
  }

  /** SQLite 数据库路径 */
  get sqlitePath(): string {
    return join(this.runDir, 'index.sqlite');
  }
}
