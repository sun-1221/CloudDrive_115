/**
 * 手动抓包模式 —— 启动带 devtools 的浏览器，用户手动操作，实时记录 115 API 请求
 *
 * 用法：
 *   yarn manual-capture
 *
 * 流程：
 *   1. 使用已有登录态启动 headful 浏览器（自带 devtools）
 *   2. 导航到文件管理页
 *   3. 用户手动操作（例如上传文件），期间终端实时输出所有 115 API 请求
 *   4. 用户完成后在终端按 Enter 结束录制
 *   5. 记录保存到 output/manual-capture/manual-<timestamp>.ndjson
 */
import { chromium } from 'playwright';
import { mkdir, writeFile, access } from 'node:fs/promises';
import path from 'node:path';

const STORAGE_STATE_PATH = 'state/auth/storage-state.json';
const OUTPUT_DIR = 'output/manual-capture';
const TARGET_URL = 'https://115.com/?cid=0&offset=0&tab=&mode=wangpan';
const IDLE_TIMEOUT_MS = 60_000; // 60 秒无新 115 API 请求即自动保存退出
const MAX_DURATION_MS = 10 * 60_000; // 最长运行 10 分钟

interface Record {
  ts: string;
  method: string;
  url: string;
  status?: number;
  requestBody?: string | null;
  responseBody?: string | null;
  responseType?: string;
}

function is115Api(url: string): boolean {
  // 包含：115 全部域名 + 阿里云 OSS（上传 callback / 直传）相关域名
  // 排除：静态资源（JS/CSS/图片/字体/sourcemap）
  const isTarget =
    /\.115\.com\//.test(url) ||
    /\.aliyuncs\.com\//.test(url) ||
    /\.aliyun\.com\//.test(url) ||
    /\.115cdn\.net\//.test(url);
  const isStatic = /\.(js|css|png|jpg|jpeg|gif|ico|woff|woff2|svg|map)(\?|$)/.test(url);
  return isTarget && !isStatic;
}

async function main(): Promise<void> {
  const runId = new Date().toISOString().replace(/[:\-T.]/g, '').slice(0, 14);
  await mkdir(OUTPUT_DIR, { recursive: true });
  const outputFile = path.resolve(OUTPUT_DIR, `manual-${runId}.ndjson`);
  const summaryFile = path.resolve(OUTPUT_DIR, `manual-${runId}-summary.md`);

  console.log('\n====================================================');
  console.log('  115 网页端手动抓包模式');
  console.log('====================================================');
  console.log(`输出文件：${outputFile}`);
  console.log(`汇总报告：${summaryFile}`);
  console.log('');

  // 检查登录态
  try {
    await access(STORAGE_STATE_PATH);
  } catch {
    console.error(`❌ 未找到登录态文件：${STORAGE_STATE_PATH}`);
    console.error('   请先执行 yarn login');
    process.exit(1);
  }

  const browser = await chromium.launch({
    headless: false,
    args: ['--start-maximized', '--auto-open-devtools-for-tabs'],
  });
  const context = await browser.newContext({
    storageState: STORAGE_STATE_PATH,
    viewport: null,
  });
  const page = await context.newPage();

  const records: Record[] = [];
  const urlToRecord = new Map<string, Record>();

  page.on('request', (req) => {
    const url = req.url();
    if (!is115Api(url)) return;
    let body: string | null = null;
    try {
      body = req.postData();
    } catch {
      body = null;
    }
    const rec: Record = {
      ts: new Date().toISOString(),
      method: req.method(),
      url,
      requestBody: body ? body.slice(0, 2000) : null,
    };
    records.push(rec);
    urlToRecord.set(`${req.method()} ${url}`, rec);
    const truncated = url.length > 120 ? url.slice(0, 120) + '…' : url;
    console.log(`→ ${req.method().padEnd(4)} ${truncated}`);
  });

  page.on('response', async (resp) => {
    const url = resp.url();
    if (!is115Api(url)) return;
    const key = `${resp.request().method()} ${url}`;
    const rec = urlToRecord.get(key);
    if (!rec || rec.status) return;
    rec.status = resp.status();
    const contentType = resp.headers()['content-type'] || '';
    rec.responseType = contentType.split(';')[0].trim();
    // 捕获 text/* + json + xml 响应体（含 OSS XML、sampleinitupload.php 的 text/html 等）
    if (/json|xml|text|plain|javascript/.test(contentType) && resp.status() < 500) {
      try {
        const text = await resp.text();
        rec.responseBody = text.length > 5000 ? text.slice(0, 5000) + '…' : text;
      } catch {
        rec.responseBody = null;
      }
    }
    const truncated = url.length > 120 ? url.slice(0, 120) + '…' : url;
    console.log(`← ${String(resp.status()).padEnd(4)} ${truncated}`);
  });

  await page.goto(TARGET_URL, { waitUntil: 'domcontentloaded' });

  console.log('');
  console.log('✓ 浏览器已打开（headful + DevTools）');
  console.log('✓ 登录态已加载，文件管理页已就绪');
  console.log('✓ 所有 115 API 请求将实时输出到此终端');
  console.log('');
  console.log('👉 请在浏览器中手动操作上传 / 转码等目标动作');
  console.log(`👉 ${IDLE_TIMEOUT_MS / 1000}s 无新 115 请求即自动保存并退出；或直接关闭浏览器窗口亦会保存`);
  console.log(`👉 最长运行 ${MAX_DURATION_MS / 60_000} 分钟`);
  console.log('');

  // 静默期 + 最大时长 + 浏览器关闭三重结束条件
  const lastActivityRef = { ts: Date.now() };
  page.on('request', (req) => {
    if (is115Api(req.url())) lastActivityRef.ts = Date.now();
  });

  const startTs = Date.now();
  const sessionEnded = await new Promise<string>((resolve) => {
    const timer = setInterval(() => {
      const now = Date.now();
      if (now - startTs > MAX_DURATION_MS) {
        clearInterval(timer);
        resolve('max-duration');
      } else if (now - lastActivityRef.ts > IDLE_TIMEOUT_MS && records.length > 0) {
        clearInterval(timer);
        resolve('idle-timeout');
      }
    }, 2_000);

    const onClose = (): void => {
      clearInterval(timer);
      resolve('browser-closed');
    };
    browser.on('disconnected', onClose);
  });

  console.log('');
  console.log(`▶ 录制结束原因：${sessionEnded}`);
  console.log(`▶ 采集到 ${records.length} 条 115 API 请求，开始保存...`);

  // 保存 NDJSON
  await writeFile(outputFile, records.map((r) => JSON.stringify(r)).join('\n'), 'utf-8');

  // 生成汇总：按 URL 聚合
  const byUrl = new Map<string, { count: number; methods: Set<string>; statuses: Set<number>; samples: Record[] }>();
  for (const r of records) {
    const u = new URL(r.url);
    const key = `${u.hostname}${u.pathname}`;
    const agg = byUrl.get(key) || { count: 0, methods: new Set(), statuses: new Set(), samples: [] };
    agg.count++;
    agg.methods.add(r.method);
    if (r.status) agg.statuses.add(r.status);
    if (agg.samples.length < 2) agg.samples.push(r);
    byUrl.set(key, agg);
  }

  const lines: string[] = [];
  lines.push(`# 手动抓包汇总 - ${runId}\n`);
  lines.push(`- 总请求数：${records.length}`);
  lines.push(`- 唯一 URL 路径数：${byUrl.size}\n`);
  lines.push('## 接口清单（按请求数降序）\n');
  lines.push('| 方法 | URL 路径 | 次数 | 状态码 |');
  lines.push('|------|---------|:---:|:---:|');
  const sorted = [...byUrl.entries()].sort((a, b) => b[1].count - a[1].count);
  for (const [url, agg] of sorted) {
    lines.push(
      `| ${[...agg.methods].join(',')} | \`${url}\` | ${agg.count} | ${[...agg.statuses].join(',')} |`,
    );
  }
  lines.push('');
  lines.push('## 响应样本（前 2 条 JSON 响应）\n');
  for (const [url, agg] of sorted) {
    const withBody = agg.samples.find((s) => s.responseBody);
    if (withBody) {
      lines.push(`### \`${url}\``);
      lines.push(`- 方法：${withBody.method}`);
      lines.push(`- URL：\`${withBody.url}\``);
      if (withBody.requestBody) {
        lines.push(`- 请求体：\n  \`\`\`\n  ${withBody.requestBody}\n  \`\`\``);
      }
      lines.push(`- 响应体（前 3000 字符）：\n  \`\`\`json\n  ${withBody.responseBody}\n  \`\`\``);
      lines.push('');
    }
  }
  await writeFile(summaryFile, lines.join('\n'), 'utf-8');

  console.log('');
  console.log(`✓ 共记录 ${records.length} 条 115 API 请求`);
  console.log(`✓ 唯一 URL 路径：${byUrl.size}`);
  console.log(`✓ NDJSON 明细：${outputFile}`);
  console.log(`✓ Markdown 汇总：${summaryFile}`);
  console.log('');

  await browser.close();
}

main().catch((err) => {
  console.error('❌ 抓包脚本失败：', err);
  process.exit(1);
});
