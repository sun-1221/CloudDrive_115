import axios from 'axios';
import type { ApiProblemDetails, SafeUiError } from '@/types';

const SENSITIVE_KEY = /(cookie|authorization|token|secret|signature|sign|account|uid|pick.?code|file.?name|filename|url|amount|money|price|balance)/i;
const SENSITIVE_VALUE = /(cookie\s*=|bearer\s+|https?:\/\/|pick.?code|signature|sign=|amount|balance)/i;

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null ? value as Record<string, unknown> : null;
}

function safeText(value: unknown, fallback: string): string {
  if (typeof value !== 'string' || !value.trim() || SENSITIVE_VALUE.test(value)) return fallback;
  return value.trim().slice(0, 240);
}

/** 只保留前端需要的 ProblemDetails 字段，并兼容 extensions.code。 */
export function parseProblemDetails(error: unknown): ApiProblemDetails | null {
  const data = axios.isAxiosError(error) ? error.response?.data : error;
  const source = asRecord(data);
  if (!source) return null;

  const extensions = asRecord(source.extensions);
  const errorsSource = asRecord(source.errors);
  const errors: Record<string, string[]> = {};
  if (errorsSource) {
    for (const [key, value] of Object.entries(errorsSource)) {
      if (SENSITIVE_KEY.test(key)) continue;
      const values = Array.isArray(value) ? value : [value];
      errors[key] = values.map(item => safeText(item, '字段校验失败'));
    }
  }

  const result: ApiProblemDetails = {};
  if (typeof source.status === 'number') result.status = source.status;
  const title = safeText(source.title, '');
  if (title) result.title = title;
  const detail = safeText(source.detail, '');
  if (detail) result.detail = detail;
  const code = typeof source.code === 'string'
    ? source.code
    : typeof extensions?.code === 'string' ? extensions.code : undefined;
  if (code) result.code = code;
  if (Object.keys(errors).length > 0) result.errors = errors;
  return result;
}

export function toSafeUiError(error: unknown): SafeUiError {
  const problem = parseProblemDetails(error);
  const status = problem?.status;
  const code = problem?.code;
  let message = problem?.detail || '请求失败，请稍后重试';

  if (status === 400 && problem?.errors && Object.keys(problem.errors).length > 0) {
    message = Object.values(problem.errors).flat().join('；');
  } else if (status === 401) message = '登录状态已失效，请重新检查连接';
  else if (status === 403) message = '当前会话没有执行此操作的权限';
  else if (status === 404) message = '请求的内容不存在或已不可用';
  else if (status === 410) message = '请求的临时内容已失效，请重新获取';
  else if (status === 429) message = '请求过于频繁，请稍后重试';
  else if (status === 501) message = '该功能暂未采集，当前不可用';
  else if (status === 502 || status === 503) message = '服务暂时不可用，请稍后重试';
  if (code === 'cloud_drive_api_not_collected') message = '该功能暂未采集，当前不可用';

  return { status, code, message };
}

/** 供日志/埋点使用：敏感字段被删除，未知结构不会原样透传。 */
export function sanitizeForTelemetry(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(sanitizeForTelemetry);
  if (value instanceof Error) return { name: value.name, message: safeText(value.message, '请求失败') };
  const record = asRecord(value);
  if (!record) return typeof value === 'string' && SENSITIVE_VALUE.test(value) ? '[已隐藏]' : value;
  const output: Record<string, unknown> = {};
  for (const [key, item] of Object.entries(record)) {
    if (SENSITIVE_KEY.test(key)) continue;
    output[key] = sanitizeForTelemetry(item);
  }
  return output;
}
