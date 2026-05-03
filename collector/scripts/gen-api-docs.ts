/**
 * 从已分析的 endpoints.json 生成 API 文档（Markdown 格式）
 *
 * 用法: npx tsx scripts/gen-api-docs.ts <runId>
 */
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const runId = process.argv[2];
if (!runId) {
  console.error('用法: npx tsx scripts/gen-api-docs.ts <runId>');
  process.exit(1);
}

const endpointsPath = resolve('output/runs', runId, 'normalized/endpoints.json');
const raw = readFileSync(endpointsPath, 'utf-8');
const endpoints: any[] = JSON.parse(raw);

// 从 examples 中提取真实 host
function getHost(ep: any): string {
  if (ep.examples?.length > 0) {
    try { return new URL(ep.examples[0].url).host; } catch { return 'unknown'; }
  }
  return 'unknown';
}

// 构建接口列表
const apis = endpoints.map((ep) => ({
  method: ep.method as string,
  host: getHost(ep),
  path: ep.normalizedPath as string,
  fullUrl: (ep.examples?.[0]?.url || '') as string,
  queryShape: ep.queryShape as Record<string, string> | null,
  bodyShape: ep.bodyShape,
  responseShape: ep.responseShape,
  authType: ep.authType as string,
  sampleCount: ep.sampleCount as number,
  statusCodes: ep.statusCodes as number[],
  scenarios: ep.scenarios as string[],
  confidence: ep.confidence as string,
}));

// 按 host 分组
const groups: Record<string, typeof apis> = {};
for (const api of apis) {
  if (!groups[api.host]) groups[api.host] = [];
  groups[api.host].push(api);
}
for (const key of Object.keys(groups)) {
  groups[key].sort((a, b) => a.path.localeCompare(b.path) || a.method.localeCompare(b.method));
}

// 生成 Markdown
const L: string[] = [];
const push = (s: string) => L.push(s);

push('# 115 网页端接口文档');
push('');
push(`> 基于自动化流量采集生成  `);
push(`> runId: \`${runId}\`  `);
push(`> 采集时间: ${new Date().toISOString().slice(0, 10)}`);
push('');

// 概览
push('## 概览');
push('');
push('| 指标 | 数值 |');
push('|---|---|');
push(`| 唯一接口数 | ${apis.length} |`);
push(`| GET 接口 | ${apis.filter((a) => a.method === 'GET').length} |`);
push(`| POST 接口 | ${apis.filter((a) => a.method === 'POST').length} |`);
push(`| 涉及域名 | ${Object.keys(groups).length} |`);
push(`| 采集场景 | ${[...new Set(apis.flatMap((a) => a.scenarios))].sort().join(', ')} |`);
push('');

// 域名排序
const domainOrder = [
  'webapi.115.com', '115.com', 'my.115.com', 'vip.115.com',
  'w.115.com', 'act.115.com', 'appversion.115.com', 'proapi.115.com',
];
const sortedKeys = Object.keys(groups).sort((a, b) => {
  const ia = domainOrder.indexOf(a);
  const ib = domainOrder.indexOf(b);
  return (ia === -1 ? 999 : ia) - (ib === -1 ? 999 : ib);
});

for (const host of sortedKeys) {
  const hostApis = groups[host];
  push('---');
  push('');
  push(`## ${host}`);
  push('');

  const isStatic = (a: typeof apis[0]) => /\.(html|js|css|vue|png|jpg|ico|php)$/.test(a.path) && !a.responseShape;
  const bizApis = hostApis.filter((a) => !isStatic(a));
  const staticApis = hostApis.filter((a) => isStatic(a));

  if (bizApis.length > 0) {
    push(`> 业务接口: ${bizApis.length} 个`);
    push('');

    for (const api of bizApis) {
      push(`### \`${api.method}\` ${api.path}`);
      push('');
      push(`| 属性 | 值 |`);
      push(`|---|---|`);
      push(`| **完整 URL** | \`${api.fullUrl.substring(0, 150)}\` |`);
      push(`| **样本数** | ${api.sampleCount} |`);
      push(`| **置信度** | ${api.confidence} |`);
      push(`| **状态码** | ${api.statusCodes.join(', ')} |`);
      push(`| **鉴权** | ${api.authType || 'cookie'} |`);
      push(`| **触发场景** | ${api.scenarios.join(', ')} |`);
      push('');

      if (api.queryShape && Object.keys(api.queryShape).length > 0) {
        push('**Query 参数:**');
        push('');
        push('| 参数名 | 类型 |');
        push('|---|---|');
        for (const [k, v] of Object.entries(api.queryShape)) {
          push(`| \`${k}\` | ${v} |`);
        }
        push('');
      }

      if (api.bodyShape && Object.keys(api.bodyShape).length > 0) {
        push('**请求体:**');
        push('');
        push('```json');
        push(JSON.stringify(api.bodyShape, null, 2));
        push('```');
        push('');
      }

      if (api.responseShape && Object.keys(api.responseShape).length > 0) {
        push('**响应体 Schema:**');
        push('');
        push('```json');
        push(JSON.stringify(api.responseShape, null, 2));
        push('```');
        push('');
      }
    }
  }

  if (staticApis.length > 0) {
    push(`### 静态资源 (${staticApis.length} 个)`);
    push('');
    push('| 方法 | 路径 | 样本数 | 场景 |');
    push('|---|---|---|---|');
    for (const api of staticApis) {
      push(`| ${api.method} | \`${api.path}\` | ${api.sampleCount} | ${api.scenarios.join(', ')} |`);
    }
    push('');
  }
}

const md = L.join('\n');
const outPath = resolve('output/runs', runId, 'reports/api-docs.md');
writeFileSync(outPath, md, 'utf-8');
console.log(`API 文档已生成: ${outPath}`);
console.log(`总接口数: ${apis.length}`);
console.log(`域名分布: ${Object.entries(groups).map(([k, v]) => `${k}: ${v.length}`).join(', ')}`);
