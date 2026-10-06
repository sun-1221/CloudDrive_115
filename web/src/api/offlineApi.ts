import httpClient from './http';
import type { CloudDriveOfflineQuotaDto, CloudDriveOfflineStatsDto, CloudDriveOfflineTaskListDto } from '@/types';

export async function listTasks(page = 1): Promise<CloudDriveOfflineTaskListDto> {
  const { data } = await httpClient.get<CloudDriveOfflineTaskListDto>('/offline/tasks', { params: { page } });
  return { ...data, items: data.items ?? [], itemSchemaStatus: data.itemSchemaStatus ?? 'not_collected' };
}
export async function getStats(): Promise<CloudDriveOfflineStatsDto> { return (await httpClient.get<CloudDriveOfflineStatsDto>('/offline/stats')).data; }
export async function getQuota(): Promise<CloudDriveOfflineQuotaDto> { return (await httpClient.get<CloudDriveOfflineQuotaDto>('/offline/quota')).data; }
