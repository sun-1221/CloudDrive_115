import httpClient from './http';
import type { CloudDriveShareListDto } from '@/types';
export async function listMyShares(offset = 0, limit = 30): Promise<CloudDriveShareListDto> {
  const { data } = await httpClient.get<CloudDriveShareListDto>('/shares', { params: { tab: 'slist', offset, limit } });
  return { ...data, items: data.items ?? [], itemSchemaStatus: data.itemSchemaStatus ?? 'available' };
}
