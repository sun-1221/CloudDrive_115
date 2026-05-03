/**
 * OpenAPI 草稿导出器 —— 从 EndpointModel 生成 OpenAPI 3.0 骨架
 */
import { writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { EndpointModel } from '../model/endpoint-model.js';

/** 生成 OpenAPI 3.0 草稿 */
export async function exportOpenApiDraft(endpoints: EndpointModel[], outputPath: string): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });

  const paths: Record<string, Record<string, unknown>> = {};

  for (const ep of endpoints) {
    // 将 normalizedPath 转为 OpenAPI 路径格式：:id → {id}
    const oaPath = ep.normalizedPath.replace(/:(\w+)/g, '{$1}');
    const method = ep.method.toLowerCase();
    if (!paths[oaPath]) paths[oaPath] = {};

    // 路径参数
    const pathParameters = ep.pathParams.map((p) => ({
      name: p.name,
      in: 'path' as const,
      required: true,
      schema: { type: 'string' },
      examples: p.examples.length > 0 ? { default: { value: p.examples[0] } } : undefined,
    }));

    // Query 参数
    const queryParameters = ep.queryShape
      ? Object.entries(ep.queryShape).map(([name, type]) => ({
          name,
          in: 'query' as const,
          required: false,
          schema: { type: mapTypeToOa(type) },
        }))
      : [];

    const parameters = [...pathParameters, ...queryParameters];

    // 请求体
    const requestBody = ep.bodyShape
      ? {
          content: {
            'application/json': {
              schema: shapeToSchema(ep.bodyShape),
            },
          },
        }
      : undefined;

    // 响应
    const responses: Record<string, unknown> = {};
    for (const status of ep.statusCodes) {
      const isError = status >= 400;
      responses[String(status)] = {
        description: isError ? '错误响应' : '成功响应',
        content: (status < 300 && ep.responseShape)
          ? { 'application/json': { schema: shapeToSchema(ep.responseShape) } }
          : (isError && ep.errorModel)
            ? { 'application/json': { schema: shapeToSchema(ep.errorModel) } }
            : undefined,
      };
    }

    if (Object.keys(responses).length === 0) {
      responses['200'] = { description: '成功响应' };
    }

    const operation: Record<string, unknown> = {
      summary: `${ep.method} ${ep.normalizedPath}`,
      operationId: generateOperationId(ep),
      tags: ep.scenarios,
      parameters: parameters.length > 0 ? parameters : undefined,
      requestBody,
      responses,
      'x-sample-count': ep.sampleCount,
      'x-confidence': ep.confidence,
      'x-auth-type': ep.authType,
    };

    // 清除 undefined 字段
    for (const key of Object.keys(operation)) {
      if (operation[key] === undefined) delete operation[key];
    }

    paths[oaPath][method] = operation;
  }

  const doc = {
    openapi: '3.0.3',
    info: {
      title: '115 Web API (Draft)',
      description: '由 115 网页端自动化采集器自动生成的 API 草稿。仅供参考，需人工校验。',
      version: '0.1.0',
    },
    servers: [
      { url: 'https://115.com', description: '115 网页端' },
      { url: 'https://aps.115.com', description: '115 API 服务' },
    ],
    paths,
  };

  await writeFile(outputPath, JSON.stringify(doc, null, 2), 'utf-8');
}

/** 生成 operationId：method + 路径摘要 */
function generateOperationId(ep: EndpointModel): string {
  const pathPart = ep.normalizedPath
    .split('/')
    .filter(Boolean)
    .map((s) => s.replace(/^:/, ''))
    .join('_');
  return `${ep.method.toLowerCase()}_${pathPart}`;
}

/** shape 对象转为 OpenAPI Schema */
function shapeToSchema(shape: Record<string, unknown>): Record<string, unknown> {
  // 特殊标记
  if (shape._type === 'array') {
    return {
      type: 'array',
      items: shape._itemShape
        ? shapeToSchema(shape._itemShape as Record<string, unknown>)
        : { type: 'object' },
    };
  }
  if (shape._type === 'deep') return { type: 'object' };
  if (shape._type === 'null') return { nullable: true };
  if (shape._type && typeof shape._type === 'string') return { type: mapTypeToOa(shape._type as string) };

  // 普通对象
  const properties: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(shape)) {
    if (key.startsWith('_')) continue;
    if (typeof value === 'string') {
      if (value.includes(' | ')) {
        properties[key] = { oneOf: value.split(' | ').map((t) => ({ type: mapTypeToOa(t.trim()) })) };
      } else {
        properties[key] = { type: mapTypeToOa(value) };
      }
    } else if (typeof value === 'object' && value !== null) {
      properties[key] = shapeToSchema(value as Record<string, unknown>);
    } else {
      properties[key] = { type: 'string' };
    }
  }

  return {
    type: 'object',
    properties: Object.keys(properties).length > 0 ? properties : undefined,
  };
}

/** 内部类型名映射为 OpenAPI type */
function mapTypeToOa(type: string): string {
  switch (type) {
    case 'number': case 'float': return 'number';
    case 'boolean': return 'boolean';
    case 'null': return 'string';
    case 'empty': return 'string';
    default: return 'string';
  }
}
