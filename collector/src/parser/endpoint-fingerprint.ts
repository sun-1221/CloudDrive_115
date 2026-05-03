/**
 * 接口指纹生成 —— 对应设计文档 7.7.2
 *
 * 三级签名体系：传输签名 → 请求签名 → 语义签名
 */
import { createHash } from 'node:crypto';

/** 语义关键字段名 */
const SEMANTIC_KEYS = ['action', 'type', 'op', 'method', 'cmd', 'command'];

/** 传输签名：method + pathname（粗粒度聚类） */
export function transportSignature(method: string, pathname: string): string {
  return `${method.toUpperCase()}:${pathname}`;
}

/** 请求签名：method + normalizedPath + queryShape + bodyShape */
export function requestSignature(
  method: string,
  normalizedPath: string,
  queryShape: Record<string, string> | null,
  bodyShape: Record<string, unknown> | null,
): string {
  const parts = [
    method.toUpperCase(),
    normalizedPath,
    queryShape ? stableStringify(queryShape) : '',
    bodyShape ? stableStringify(bodyShape) : '',
  ];
  return hashString(parts.join('|'));
}

/** 语义签名：当请求体含 action/type/op 等字段时，将其值并入签名 */
export function semanticSignature(
  method: string,
  normalizedPath: string,
  body: Record<string, unknown> | null,
): string | null {
  if (!body) return null;

  const semanticValues: string[] = [];
  for (const key of SEMANTIC_KEYS) {
    if (key in body && typeof body[key] === 'string') {
      semanticValues.push(`${key}=${body[key]}`);
    }
  }

  if (semanticValues.length === 0) return null;

  return hashString(`${method.toUpperCase()}:${normalizedPath}:${semanticValues.join('&')}`);
}

function stableStringify(obj: Record<string, unknown>): string {
  const sorted = Object.keys(obj).sort();
  return JSON.stringify(obj, sorted);
}

function hashString(input: string): string {
  return createHash('sha256').update(input).digest('hex').slice(0, 16);
}
