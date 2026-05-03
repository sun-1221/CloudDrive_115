/**
 * CSV 导出器
 */
import { writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { EndpointModel } from '../model/endpoint-model.js';
import type { LedgerEntry } from '../model/report-model.js';

const CSV_HEADER = 'module,page,action,method,path,queryParams,bodyStructure,authType,responseStructure,statusCodes,sampleCount,notes';

/** 从 EndpointModel 数组导出 CSV */
export async function exportCsv(endpoints: EndpointModel[], outputPath: string): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });
  const entries = endpoints.map(endpointToLedger);

  const rows = entries.map((e) =>
    [e.module, e.page, e.action, e.method, e.path, e.queryParams, e.bodyStructure, e.authType, e.responseStructure, e.statusCodes, e.sampleCount, e.notes]
      .map((v) => `"${String(v).replace(/"/g, '""')}"`)
      .join(','),
  );

  await writeFile(outputPath, [CSV_HEADER, ...rows].join('\n'), 'utf-8');
}

/** EndpointModel → LedgerEntry 转换 */
export function endpointToLedger(ep: EndpointModel): LedgerEntry {
  // 从路径推断模块名
  const pathParts = ep.normalizedPath.split('/').filter(Boolean);
  const module = pathParts[0] ?? 'unknown';

  return {
    module,
    page: ep.scenarios.join(', '),
    action: ep.examples[0]?.scenarioId ?? '',
    method: ep.method,
    path: ep.normalizedPath,
    queryParams: ep.queryShape ? Object.keys(ep.queryShape).join(', ') : '',
    bodyStructure: ep.bodyShape ? compactShape(ep.bodyShape) : '',
    authType: ep.authType,
    responseStructure: ep.responseShape ? compactShape(ep.responseShape) : '',
    statusCodes: ep.statusCodes.join(', '),
    sampleCount: ep.sampleCount,
    notes: ep.confidence === 'low' ? '需人工复核' : '',
  };
}

/** 将 shape 对象压缩为简短字符串 */
function compactShape(shape: Record<string, unknown>): string {
  const keys = Object.keys(shape);
  if (keys.length <= 5) {
    return keys.map((k) => `${k}:${typeof shape[k] === 'string' ? shape[k] : 'obj'}`).join(', ');
  }
  return `${keys.slice(0, 4).join(', ')} +${keys.length - 4} more`;
}
