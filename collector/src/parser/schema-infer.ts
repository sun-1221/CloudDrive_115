/**
 * Schema 推断 —— 从多个样本中推断稳定的字段结构
 */

/** 合并多个 shape 样本，取字段交集并联合类型 */
export function mergeShapes(shapes: Array<Record<string, unknown> | null>): Record<string, unknown> | null {
  const valid = shapes.filter((s): s is Record<string, unknown> => s !== null);
  if (valid.length === 0) return null;
  if (valid.length === 1) return valid[0];

  // 统计每个字段出现的次数和类型
  const fieldStats = new Map<string, { count: number; types: Set<string>; values: Set<string> }>();

  for (const shape of valid) {
    for (const [key, value] of Object.entries(shape)) {
      const stat = fieldStats.get(key) ?? { count: 0, types: new Set(), values: new Set() };
      stat.count++;
      if (typeof value === 'string') {
        stat.types.add(value);
      } else if (typeof value === 'object' && value !== null) {
        stat.types.add('object');
        stat.values.add(JSON.stringify(value));
      }
      fieldStats.set(key, stat);
    }
  }

  // 仅保留出现率 >= 50% 的字段
  const threshold = valid.length * 0.5;
  const merged: Record<string, unknown> = {};

  for (const [key, stat] of fieldStats.entries()) {
    if (stat.count < threshold) continue;

    if (stat.types.size === 1) {
      merged[key] = [...stat.types][0];
    } else if (stat.types.has('object') && stat.values.size === 1) {
      // 所有样本中该字段的对象结构相同
      merged[key] = JSON.parse([...stat.values][0]);
    } else {
      merged[key] = [...stat.types].join(' | ');
    }
  }

  return Object.keys(merged).length > 0 ? merged : null;
}

/** 合并多组 queryShape，取字段并集并联合类型 */
export function mergeQueryShapes(shapes: Array<Record<string, string>>): Record<string, string> | null {
  if (shapes.length === 0) return null;

  const fieldTypes = new Map<string, Set<string>>();
  for (const shape of shapes) {
    for (const [key, type] of Object.entries(shape)) {
      const types = fieldTypes.get(key) ?? new Set();
      types.add(type);
      fieldTypes.set(key, types);
    }
  }

  const merged: Record<string, string> = {};
  for (const [key, types] of fieldTypes.entries()) {
    merged[key] = types.size === 1 ? [...types][0] : [...types].join(' | ');
  }

  return Object.keys(merged).length > 0 ? merged : null;
}
