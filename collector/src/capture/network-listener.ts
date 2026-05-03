/**
 * 网络监听器 —— 采集层核心，对应设计文档 7.6
 *
 * 负责在 Page 上注册 request/response 事件监听，
 * 将每次请求/响应交互转为 CaptureRecord 并写入存储。
 */
import type { Page, Request, Response } from 'playwright';
import type { CaptureRecord } from '../model/capture-record.js';
import type { NdjsonStore } from '../storage/ndjson-store.js';
import type { Logger } from 'pino';
import { nanoid } from 'nanoid';
import { createHash } from 'node:crypto';

/** 采集会话：供场景层标注当前动作上下文 */
export interface CaptureSession {
  /** 设置当前正在执行的动作名称 */
  setCurrentAction(actionName: string): void;
  /** 设置当前步骤 ID */
  setCurrentStep(stepId: string): void;
  /** 获取本会话采集到的记录数 */
  getCapturedCount(): number;
}

/** 仅采集的资源类型 */
const CAPTURED_RESOURCE_TYPES = new Set(['xhr', 'fetch', 'document']);

/** 二进制 Content-Type 前缀 */
const BINARY_PREFIXES = ['image/', 'video/', 'audio/', 'font/', 'application/octet-stream', 'application/zip'];

export interface NetworkListenerOptions {
  runId: string;
  scenarioId: string;
  allowedDomains: string[];
  captureResponseBody: boolean;
  maxBodySize: number;
  logger: Logger;
  store: NdjsonStore;
}

export class NetworkListener implements CaptureSession {
  private currentAction: string | null = null;
  private currentStep: string | null = null;
  private capturedCount = 0;
  private readonly pendingRequests = new Map<string, { record: Partial<CaptureRecord>; startTime: number }>();

  constructor(private readonly options: NetworkListenerOptions) {}

  /** 在指定 Page 上注册监听 */
  attach(page: Page): void {
    page.on('request', (req) => this.onRequest(req));
    page.on('response', (resp) => void this.onResponse(resp));
    page.on('requestfailed', (req) => void this.onRequestFailed(req));
  }

  setCurrentAction(actionName: string): void {
    this.currentAction = actionName;
  }

  setCurrentStep(stepId: string): void {
    this.currentStep = stepId;
  }

  getCapturedCount(): number {
    return this.capturedCount;
  }

  private onRequest(request: Request): void {
    const urlStr = request.url();
    if (urlStr.startsWith('data:') || urlStr.startsWith('blob:')) return;

    let parsed: URL;
    try { parsed = new URL(urlStr); } catch { return; }

    if (!this.isDomainAllowed(parsed.hostname)) return;

    const resourceType = request.resourceType();
    if (!CAPTURED_RESOURCE_TYPES.has(resourceType)) return;

    const id = nanoid();
    let requestBody: string | null = null;
    let requestSize: number | null = null;
    try {
      requestBody = request.postData() ?? null;
      requestSize = requestBody ? Buffer.byteLength(requestBody) : null;
    } catch { /* ignore */ }

    const record: Partial<CaptureRecord> = {
      id,
      runId: this.options.runId,
      scenarioId: this.options.scenarioId,
      scenarioStepId: this.currentStep,
      networkId: urlStr, // Playwright 没有独立的 network ID，用 URL 作为关联键
      pageUrl: request.frame()?.url() ?? '',
      frameUrl: request.frame()?.parentFrame() ? request.frame()?.url() ?? null : null,
      actionName: this.currentAction,
      resourceType,
      method: request.method(),
      url: urlStr,
      pathname: parsed.pathname,
      query: parsed.search || null,
      initiator: null,
      redirectChain: request.redirectedFrom()
        ? this.buildRedirectChain(request)
        : null,
      requestHeaders: request.headers(),
      requestBody,
      requestSize,
      startedAt: new Date().toISOString(),
    };

    // 用请求唯一键关联后续响应
    const key = this.requestKey(request);
    this.pendingRequests.set(key, { record, startTime: Date.now() });
  }

  private async onResponse(response: Response): Promise<void> {
    const request = response.request();
    const key = this.requestKey(request);
    const pending = this.pendingRequests.get(key);
    if (!pending) return;

    this.pendingRequests.delete(key);
    const { record, startTime } = pending;
    const now = Date.now();
    const contentType = response.headers()['content-type'] ?? null;
    const isBinary = contentType ? BINARY_PREFIXES.some((p) => contentType.startsWith(p)) : false;

    record.status = response.status();
    record.responseHeaders = response.headers();
    record.contentType = contentType;
    record.isBinary = isBinary;
    record.endedAt = new Date().toISOString();
    record.durationMs = now - startTime;
    record.fromCache = response.fromServiceWorker();
    record.failedReason = null;

    // 响应体采集
    let responseBody: string | null = null;
    let responseSize: number | null = null;
    let bodyTruncated = false;
    let contentHash: string | null = null;

    if (this.options.captureResponseBody && !isBinary) {
      try {
        const bodyBuf = await response.body();
        responseSize = bodyBuf.byteLength;
        if (responseSize > this.options.maxBodySize) {
          responseBody = bodyBuf.subarray(0, this.options.maxBodySize).toString('utf-8');
          bodyTruncated = true;
        } else {
          responseBody = bodyBuf.toString('utf-8');
        }
        contentHash = createHash('sha256').update(bodyBuf).digest('hex').slice(0, 16);
      } catch {
        // 某些响应（如 204、重定向）没有 body
      }
    }

    record.responseBody = responseBody;
    record.responseSize = responseSize;
    record.bodyTruncated = bodyTruncated;
    record.contentHash = contentHash;

    this.writeRecord(record as CaptureRecord);
  }

  private onRequestFailed(request: Request): void {
    const key = this.requestKey(request);
    const pending = this.pendingRequests.get(key);
    if (!pending) return;

    this.pendingRequests.delete(key);
    const { record, startTime } = pending;

    record.status = null;
    record.responseHeaders = null;
    record.responseBody = null;
    record.responseSize = null;
    record.contentType = null;
    record.isBinary = false;
    record.bodyTruncated = false;
    record.contentHash = null;
    record.endedAt = new Date().toISOString();
    record.durationMs = Date.now() - startTime;
    record.fromCache = false;
    record.failedReason = request.failure()?.errorText ?? 'unknown';

    this.writeRecord(record as CaptureRecord);
    this.options.logger.warn({ url: request.url(), failure: record.failedReason }, '请求失败');
  }

  private writeRecord(record: CaptureRecord): void {
    record.capturedAt = new Date().toISOString();
    this.options.store.write(record as unknown as Record<string, unknown>);
    this.capturedCount++;

    this.options.logger.debug(
      { method: record.method, pathname: record.pathname, status: record.status },
      '记录已写入',
    );
  }

  private isDomainAllowed(hostname: string): boolean {
    return this.options.allowedDomains.some(
      (domain) => hostname === domain || hostname.endsWith(`.${domain}`),
    );
  }

  /** 为 request 生成唯一关联键 */
  private requestKey(request: Request): string {
    return `${request.method()}:${request.url()}:${request.postData()?.slice(0, 64) ?? ''}`;
  }

  /** 构建重定向链 */
  private buildRedirectChain(request: Request): string[] {
    const chain: string[] = [];
    let current = request.redirectedFrom();
    while (current) {
      chain.unshift(current.url());
      current = current.redirectedFrom();
    }
    return chain.length > 0 ? chain : null as unknown as string[];
  }
}
