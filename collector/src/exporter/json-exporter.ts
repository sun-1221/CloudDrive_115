/**
 * JSON 导出器
 */
import { writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { EndpointModel } from '../model/endpoint-model.js';

export async function exportJson(endpoints: EndpointModel[], outputPath: string): Promise<void> {
  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, JSON.stringify(endpoints, null, 2), 'utf-8');
}
