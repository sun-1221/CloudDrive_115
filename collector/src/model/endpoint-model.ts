/**
 * 接口聚合模型 —— 对应设计文档 8.5 EndpointModel
 *
 * 由 parser 层从多条 CaptureRecord 归一化、聚合后生成。
 * 代表一个唯一的 API 端点。
 */
export interface EndpointModel {
  /** HTTP 方法 */
  method: string;
  /** 归一化后的路径（路径参数已替换为占位符） */
  normalizedPath: string;
  /** 传输签名：method + pathname，粗粒度聚类 */
  transportSignature: string;
  /** 请求签名：method + normalizedPath + queryShape + bodyShape，主要去重依据 */
  requestSignature: string;
  /** 语义签名：含 action/type/op 等关键字段，避免网关式接口误合并 */
  semanticSignature: string | null;
  /** 路径中识别出的参数位置与名称 */
  pathParams: PathParam[];
  /** Query 参数的结构摘要：仅包含语义参数的键名与类型 */
  queryShape: Record<string, string> | null;
  /** 请求体的字段结构摘要：包含键名、类型和关键语义字段值 */
  bodyShape: Record<string, unknown> | null;
  /** queryShape + bodyShape + 关键 header 的组合签名，用于接口去重 */
  requestShape: string;
  /** 响应体字段结构摘要 */
  responseShape: Record<string, unknown> | null;
  /** 鉴权方式：cookie / bearer / custom-signature / none */
  authType: AuthType;
  /** 样本总数 */
  sampleCount: number;
  /** 出现过的状态码集合 */
  statusCodes: number[];
  /** 出现过的场景 ID 集合 */
  scenarios: string[];
  /** 请求/响应样本示例（保留少量代表性样本） */
  examples: EndpointExample[];
  /** 错误响应结构摘要 */
  errorModel: Record<string, unknown> | null;
  /** 首次出现时间 ISO 8601 */
  firstSeenAt: string;
  /** 最后出现时间 ISO 8601 */
  lastSeenAt: string;
  /** 聚合置信度：high / medium / low */
  confidence: Confidence;
  /** 关联的原始记录 ID 列表 */
  sampleRefs: string[];
  /** 人工备注 */
  notes: string | null;
}

export interface PathParam {
  /** 参数在路径中的位置索引 */
  index: number;
  /** 推测的参数名称 */
  name: string;
  /** 出现过的示例值 */
  examples: string[];
}

export interface EndpointExample {
  /** 关联的 CaptureRecord ID */
  recordId: string;
  /** 场景 ID */
  scenarioId: string;
  /** 请求 URL */
  url: string;
  /** 状态码 */
  status: number | null;
  /** 请求体摘要 */
  requestBodyPreview: string | null;
  /** 响应体摘要 */
  responseBodyPreview: string | null;
}

export type AuthType = 'cookie' | 'bearer' | 'custom-signature' | 'none' | 'unknown';

export type Confidence = 'high' | 'medium' | 'low';
