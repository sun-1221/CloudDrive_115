/**
 * 脱敏处理 —— 对应设计文档 12.2 + 12.4
 */

/** 需要脱敏的 header 名称 */
const SENSITIVE_HEADERS = new Set([
  'cookie', 'authorization', 'set-cookie',
  'x-auth-token', 'x-csrf-token',
]);

/** 脱敏 headers */
export function redactHeaders(headers: Record<string, string>): Record<string, string> {
  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(headers)) {
    result[key] = SENSITIVE_HEADERS.has(key.toLowerCase())
      ? maskValue(value)
      : value;
  }
  return result;
}

/** 需要脱敏的 body 字段名（不区分大小写） */
const SENSITIVE_BODY_KEYS = new Set([
  'password', 'passwd', 'token', 'access_token', 'refresh_token',
  'secret', 'credential', 'api_key', 'apikey', 'private_key',
]);

/** 脱敏 JSON 字符串中的敏感字段 */
export function redactBody(body: string | null): string | null {
  if (!body) return null;
  try {
    const parsed = JSON.parse(body);
    if (typeof parsed === 'object' && parsed !== null) {
      return JSON.stringify(redactObject(parsed));
    }
  } catch {
    // 非 JSON body，做正则脱敏
    return redactRawText(body);
  }
  return body;
}

/** 递归脱敏对象中的敏感字段 */
function redactObject(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(obj)) {
    if (SENSITIVE_BODY_KEYS.has(key.toLowerCase())) {
      result[key] = typeof value === 'string' ? maskValue(value) : '***';
    } else if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      result[key] = redactObject(value as Record<string, unknown>);
    } else {
      result[key] = value;
    }
  }
  return result;
}

/** 对非 JSON 文本做基础正则脱敏 */
function redactRawText(text: string): string {
  return text.replace(
    /(password|token|secret|api_key|apikey)=([^&\s]+)/gi,
    (_, key) => `${key}=***`,
  );
}

/** 遮蔽敏感值：保留前 4 后 4，中间用 *** 替换 */
function maskValue(value: string): string {
  if (value.length <= 12) return '***';
  return `${value.slice(0, 4)}***${value.slice(-4)}`;
}
