#!/usr/bin/env node
/**
 * CLI 入口 —— 对应设计文档 7.1
 */

// Windows 下设置控制台代码页为 UTF-8（65001），避免中文乱码
if (process.platform === 'win32') {
  try {
    const { execSync } = await import('node:child_process');
    execSync('chcp 65001', { stdio: 'ignore' });
  } catch { /* ignore */ }
}

import { Command } from 'commander';
import { registerLoginCommand } from './commands/login.js';
import { registerCollectCommand } from './commands/collect.js';
import { registerReplayCommand } from './commands/replay.js';
import { registerAnalyzeCommand } from './commands/analyze.js';
import { registerExportCommand } from './commands/export.js';

const program = new Command();

program
  .name('collector')
  .description('115 网页端自动化接口采集器')
  .version('0.1.0');

registerLoginCommand(program);
registerCollectCommand(program);
registerReplayCommand(program);
registerAnalyzeCommand(program);
registerExportCommand(program);

program.parse();
