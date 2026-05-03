import httpClient from './http';
import type { SearchFileInput, CloudDriveSearchResultDto, PagedResult } from '@/types';

export async function searchFiles(
  input: SearchFileInput,
): Promise<PagedResult<CloudDriveSearchResultDto>> {
  const response = await httpClient.get<PagedResult<CloudDriveSearchResultDto>>('/files/search', {
    params: {
      cloudAccountUid: input.cloudAccountUid,
      keyword: input.keyword,
      offset: input.offset,
      limit: input.limit,
    },
  });
  return response.data;
}
