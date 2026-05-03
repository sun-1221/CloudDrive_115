/**
 * 配置加载 —— 从文件、环境变量和 CLI 参数合并配置
 */
import { config as loadEnv } from 'dotenv';
import { CollectorConfigSchema, type CollectorConfig } from './schema.js';
import { mergeConfig } from './merge-config.js';

export interface LoadConfigOptions {
  /** 配置文件路径（可选） */
  configPath?: string;
  /** CLI 传入的覆盖项 */
  overrides?: Partial<CollectorConfig>;
}

/**
 * 加载并校验配置
 *
 * 优先级：CLI overrides > 环境变量 > 配置文件 > 默认值
 */
export async function loadConfig(options: LoadConfigOptions = {}): Promise<CollectorConfig> {
  loadEnv();

  const fileConfig = options.configPath
    ? await loadConfigFile(options.configPath)
    : {};

  const envConfig = loadEnvConfig();
  const merged = mergeConfig(fileConfig, envConfig, options.overrides ?? {});

  return CollectorConfigSchema.parse(merged);
}

async function loadConfigFile(path: string): Promise<Record<string, unknown>> {
  try {
    const mod = await import(path);
    return mod.default ?? mod;
  } catch {
    return {};
  }
}

function loadEnvConfig(): Record<string, unknown> {
  const env: Record<string, unknown> = {};

  if (process.env.BASE_URL) env.baseUrl = process.env.BASE_URL;
  if (process.env.HEADLESS) env.headless = process.env.HEADLESS === 'true';
  if (process.env.STORAGE_STATE_PATH) env.storageStatePath = process.env.STORAGE_STATE_PATH;
  if (process.env.OUTPUT_DIR) env.outputDir = process.env.OUTPUT_DIR;

  return env;
}
