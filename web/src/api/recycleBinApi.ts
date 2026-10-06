import httpClient from './http';
import type {
  GetRecycleBinListInput,
  CloudDriveRecycleBinItemDto,
  PagedResult,
} from '@/types';

export async function getRecycleBinList(
  input: GetRecycleBinListInput,
): Promise<PagedResult<CloudDriveRecycleBinItemDto>> {
  const response = await httpClient.get<PagedResult<CloudDriveRecycleBinItemDto>>('/recycle', {
    params: {
      offset: input.offset,
      limit: input.limit,
    },
  });
  return response.data;
}
