/**
 * 多层配置合并
 *
 * 优先级：overrides > env > file（后者覆盖前者）
 */
export function mergeConfig(
  ...sources: Record<string, unknown>[]
): Record<string, unknown> {
  const result: Record<string, unknown> = {};

  for (const source of sources) {
    for (const [key, value] of Object.entries(source)) {
      if (value !== undefined && value !== null) {
        result[key] = value;
      }
    }
  }

  return result;
}
