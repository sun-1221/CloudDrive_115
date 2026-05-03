/**
 * 初始化脚本 —— 创建必要的目录结构
 */
import { mkdir } from 'node:fs/promises';

const dirs = [
  'state/auth',
  'output/runs',
];

async function setup(): Promise<void> {
  for (const dir of dirs) {
    await mkdir(dir, { recursive: true });
    console.log(`✓ ${dir}`);
  }
  console.log('初始化完成');
}

setup().catch(console.error);
