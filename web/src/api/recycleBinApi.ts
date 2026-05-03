import httpClient from './http';
import type {
  GetRecycleBinListInput,
  RecycleBinOperationInput,
  CloudDriveRecycleBinItemDto,
  CloudDriveOperationResultDto,
  PagedResult,
} from '@/types';

export async function getRecycleBinList(
  input: GetRecycleBinListInput,
): Promise<PagedResult<CloudDriveRecycleBinItemDto>> {
  const response = await httpClient.get<PagedResult<CloudDriveRecycleBinItemDto>>('/recycle-bin', {
    params: {
      cloudAccountUid: input.cloudAccountUid,
      offset: input.offset,
      limit: input.limit,
    },
  });
  return response.data;
}

export async function restoreFiles(
  input: RecycleBinOperationInput,
): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>(
    '/recycle-bin/restore',
    input,
  );
  return response.data;
}

export async function permanentDelete(
  input: RecycleBinOperationInput,
): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>(
    '/recycle-bin/permanent-delete',
    input,
  );
  return response.data;
}

export async function clearRecycleBin(
  cloudAccountUid: string,
): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>(
    '/recycle-bin/clear',
    { cloudAccountUid },
  );
  return response.data;
}
