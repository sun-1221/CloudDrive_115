/**
 * 原始采集记录 —— 对应设计文档 8.4 CaptureRecord
 *
 * 每条记录对应一次 HTTP 请求/响应交互，是整个系统的事实真相源。
 * 所有归一化、聚合和导出操作都基于此记录派生。
 */
export interface CaptureRecord {
  /** 记录唯一 ID */
  id: string;
  /** 所属 run 的唯一标识 */
  runId: string;
  /** 触发该请求的场景 ID */
  scenarioId: string;
  /** 触发该请求的场景步骤 ID */
  scenarioStepId: string | null;
  /** Playwright CDP 网络请求 ID */
  networkId: string;
  /** 发起请求时的页面 URL */
  pageUrl: string;
  /** 发起请求的 frame URL（若非主 frame） */
  frameUrl: string | null;
  /** 触发该请求的动作名称 */
  actionName: string | null;
  /** Playwright 资源类型：document / xhr / fetch / script / stylesheet 等 */
  resourceType: string;
  /** HTTP 方法 */
  method: string;
  /** 完整请求 URL */
  url: string;
  /** URL pathname 部分 */
  pathname: string;
  /** URL query string（原始值） */
  query: string | null;
  /** 请求发起者信息（如 script URL 或 stack trace 摘要） */
  initiator: string | null;
  /** 重定向链（3xx 跳转路径） */
  redirectChain: string[] | null;
  /** 请求头 */
  requestHeaders: Record<string, string>;
  /** 请求体（文本形式；二进制不保存） */
  requestBody: string | null;
  /** 请求体大小（字节） */
  requestSize: number | null;
  /** HTTP 响应状态码 */
  status: number | null;
  /** 响应头 */
  responseHeaders: Record<string, string> | null;
  /** 响应体（文本形式；超限或二进制时为 null） */
  responseBody: string | null;
  /** 响应体大小（字节） */
  responseSize: number | null;
  /** 响应 Content-Type */
  contentType: string | null;
  /** 是否为二进制内容 */
  isBinary: boolean;
  /** 响应体是否因超限被截断 */
  bodyTruncated: boolean;
  /** 响应体内容哈希（SHA-256 前 16 位） */
  contentHash: string | null;
  /** 请求失败原因（仅失败时有值） */
  failedReason: string | null;
  /** 请求开始时间 ISO 8601 */
  startedAt: string;
  /** 请求结束时间 ISO 8601 */
  endedAt: string | null;
  /** 请求耗时（毫秒） */
  durationMs: number | null;
  /** 是否从浏览器缓存返回 */
  fromCache: boolean;
  /** 记录写入时间 ISO 8601 */
  capturedAt: string;
}
