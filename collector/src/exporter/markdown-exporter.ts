/**
 * Markdown 报告导出器
 */
import { writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { RunManifest } from '../model/report-model.js';
import type { EndpointModel } from '../model/endpoint-model.js';

/** 导出 RunManifest 的采集摘要报告 */
export async function exportMarkdownSummary(manifest: RunManifest, outputPath: string): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });

  const lines: string[] = [
    `# 采集报告 - ${manifest.runId}`,
    '',
    `- **目标**: ${manifest.targetName}`,
    `- **入口**: ${manifest.baseUrl}`,
    `- **开始**: ${manifest.startedAt}`,
    `- **结束**: ${manifest.endedAt ?? '进行中'}`,
    `- **状态**: ${manifest.exitStatus}`,
    '',
    '## 统计',
    '',
    `| 指标 | 值 |`,
    `|---|---|`,
    `| 总请求数 | ${manifest.counters.totalRequests} |`,
    `| 目标域名请求 | ${manifest.counters.targetDomainRequests} |`,
    `| 去重接口数 | ${manifest.counters.uniqueEndpoints ?? '-'} |`,
    `| 成功场景 | ${manifest.counters.passedScenarios} |`,
    `| 失败场景 | ${manifest.counters.failedScenarios} |`,
    `| 跳过场景 | ${manifest.counters.skippedScenarios} |`,
    '',
    '## 场景结果',
    '',
    '| 场景 | 状态 | 采集数 | 耗时 | 原因 |',
    '|---|---|---|---|---|',
    ...manifest.scenarioResults.map((r) =>
      `| ${r.scenarioId} | ${r.status} | ${r.capturedCount} | ${r.durationMs}ms | ${r.reason ?? '-'} |`,
    ),
  ];

  await writeFile(outputPath, lines.join('\n'), 'utf-8');
}

/** 导出 EndpointModel 数组的接口台账 Markdown */
export async function exportMarkdown(endpoints: EndpointModel[], outputPath: string): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });

  const byMethod = new Map<string, EndpointModel[]>();
  for (const ep of endpoints) {
    const group = byMethod.get(ep.method) ?? [];
    group.push(ep);
    byMethod.set(ep.method, group);
  }

  const lines: string[] = [
    `# 接口台账`,
    '',
    `> 共 ${endpoints.length} 个接口`,
    '',
    '## 总览',
    '',
    '| 方法 | 接口数 |',
    '|---|---|',
    ...[...byMethod.entries()].map(([m, eps]) => `| ${m} | ${eps.length} |`),
    '',
  ];

  // 按方法分组列出
  for (const [method, eps] of byMethod) {
    lines.push(`## ${method}`, '');
    lines.push('| 路径 | 鉴权 | 状态码 | 样本数 | 场景 | 置信度 |');
    lines.push('|---|---|---|---|---|---|');

    for (const ep of eps) {
      lines.push(
        `| \`${ep.normalizedPath}\` | ${ep.authType} | ${ep.statusCodes.join(',')} | ${ep.sampleCount} | ${ep.scenarios.join(',')} | ${ep.confidence} |`,
      );
    }
    lines.push('');
  }

  // 低置信度接口清单
  const lowConf = endpoints.filter((e) => e.confidence === 'low');
  if (lowConf.length > 0) {
    lines.push('## 待复核（低置信度）', '');
    lines.push('| 方法 | 路径 | 样本数 | 原因 |');
    lines.push('|---|---|---|---|');
    for (const ep of lowConf) {
      lines.push(`| ${ep.method} | \`${ep.normalizedPath}\` | ${ep.sampleCount} | 单样本 |`);
    }
    lines.push('');
  }

  await writeFile(outputPath, lines.join('\n'), 'utf-8');
}

/** 导出场景覆盖报告：场景 → 接口映射 + 覆盖率统计 */
export async function exportCoverageReport(
  endpoints: EndpointModel[],
  outputPath: string,
): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });

  // 构建场景 → 接口映射
  const scenarioMap = new Map<string, EndpointModel[]>();
  for (const ep of endpoints) {
    for (const sid of ep.scenarios) {
      const list = scenarioMap.get(sid) ?? [];
      list.push(ep);
      scenarioMap.set(sid, list);
    }
  }

  // 统计
  const totalEndpoints = endpoints.length;
  const multiScenario = endpoints.filter((e) => e.scenarios.length > 1).length;
  const singleScenario = endpoints.filter((e) => e.scenarios.length === 1).length;

  const lines: string[] = [
    '# 场景覆盖报告',
    '',
    '## 总览',
    '',
    '| 指标 | 值 |',
    '|---|---|',
    `| 总接口数 | ${totalEndpoints} |`,
    `| 多场景覆盖接口 | ${multiScenario} |`,
    `| 单场景独占接口 | ${singleScenario} |`,
    `| 场景数 | ${scenarioMap.size} |`,
    '',
  ];

  // 每个场景的详细接口列表
  for (const [scenarioId, eps] of scenarioMap) {
    const sorted = [...eps].sort((a, b) => a.normalizedPath.localeCompare(b.normalizedPath));
    lines.push(`## 场景: ${scenarioId}`, '');
    lines.push(`> 共 ${eps.length} 个接口`);
    lines.push('');
    lines.push('| 方法 | 路径 | 状态码 | 样本数 | 独占 |');
    lines.push('|---|---|---|---|---|');

    for (const ep of sorted) {
      const exclusive = ep.scenarios.length === 1 ? '✓' : '';
      lines.push(
        `| ${ep.method} | \`${ep.normalizedPath}\` | ${ep.statusCodes.join(',')} | ${ep.sampleCount} | ${exclusive} |`,
      );
    }
    lines.push('');
  }

  // 跨场景共享接口
  const shared = endpoints.filter((e) => e.scenarios.length > 1);
  if (shared.length > 0) {
    lines.push('## 跨场景共享接口', '');
    lines.push('| 方法 | 路径 | 场景 | 样本数 |');
    lines.push('|---|---|---|---|');
    for (const ep of shared) {
      lines.push(
        `| ${ep.method} | \`${ep.normalizedPath}\` | ${ep.scenarios.join(', ')} | ${ep.sampleCount} |`,
      );
    }
    lines.push('');
  }

  await writeFile(outputPath, lines.join('\n'), 'utf-8');
}
