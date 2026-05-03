/**
 * 响应归一化 —— 推断响应体的字段结构
 */

/** 从 JSON 响应体推断字段结构摘要 */
export function inferResponseShape(body: string | null): Record<string, unknown> | null {
  if (!body) return null;

  try {
    const parsed = JSON.parse(body);
    return inferShape(parsed);
  } catch {
    return null;
  }
}

/** 递归推断 JSON 结构（只保留键名和类型，不保留具体值） */
function inferShape(value: unknown, depth = 0): Record<string, unknown> | null {
  if (depth > 3) return { _type: 'deep' };

  if (value === null) return { _type: 'null' };
  if (Array.isArray(value)) {
    const first = value[0];
    return { _type: 'array', _itemShape: first ? inferShape(first, depth + 1) : null, _length: value.length };
  }
  if (typeof value === 'object') {
    const shape: Record<string, unknown> = {};
    for (const [key, val] of Object.entries(value as Record<string, unknown>)) {
      shape[key] = typeof val === 'object' && val !== null
        ? inferShape(val, depth + 1)
        : typeof val;
    }
    return shape;
  }
  return { _type: typeof value };
}
