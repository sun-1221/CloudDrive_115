import httpClient from './http';
import type {
  FileListResult,
  GetFileListInput,
  CreateFolderInput,
  RenameFileInput,
  MoveFilesInput,
  CopyFilesInput,
  DeleteFilesInput,
  CloudDriveOperationResultDto,
  CloudDriveUploadExecutionDto,
  CloudDriveFileDetailDto,
  CloudDriveDownloadDto,
  CloudDriveFolderDto,
  PagedResult,
  UploadProgressEvent,
} from '@/types';

export async function getFileList(input: GetFileListInput): Promise<FileListResult> {
  const response = await httpClient.get<FileListResult>('/files', {
    params: {
      cloudAccountUid: input.cloudAccountUid,
      directoryId: input.directoryId,
      offset: input.offset,
      limit: input.limit,
      sortField: input.sortField,
      sortOrder: input.sortOrder,
      fileTypeFilter: input.fileTypeFilter,
    },
  });
  return response.data;
}

export async function getFileDetail(
  cloudAccountUid: string,
  fileId: string,
): Promise<CloudDriveFileDetailDto> {
  const response = await httpClient.get<CloudDriveFileDetailDto>(`/files/${fileId}`, {
    params: { cloudAccountUid },
  });
  return response.data;
}

export async function createFolder(input: CreateFolderInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/folder', input);
  return response.data;
}

export async function renameFile(input: RenameFileInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.put<CloudDriveOperationResultDto>(
    `/files/${input.fileId}/rename`,
    input,
  );
  return response.data;
}

export async function moveFiles(input: MoveFilesInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/move', input);
  return response.data;
}

export async function copyFiles(input: CopyFilesInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/copy', input);
  return response.data;
}

export async function deleteFiles(input: DeleteFilesInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/delete', input);
  return response.data;
}

export async function getDownloadUrl(
  cloudAccountUid: string,
  fileId: string,
): Promise<CloudDriveDownloadDto> {
  const response = await httpClient.get<CloudDriveDownloadDto>(`/files/${fileId}/download`, {
    params: { cloudAccountUid },
  });
  return response.data;
}

export async function upload(
  file: File,
  cloudAccountUid: string,
  directoryId: string,
  forceAsync: boolean = false,
  onProgress?: (event: UploadProgressEvent) => void,
  signal?: AbortSignal,
): Promise<CloudDriveUploadExecutionDto> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('cloudAccountUid', cloudAccountUid);
  formData.append('directoryId', directoryId);
  formData.append('forceAsync', String(forceAsync));

  const response = await httpClient.post<CloudDriveUploadExecutionDto>(
    '/files/upload',
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          onProgress({
            loaded: progressEvent.loaded,
            total: progressEvent.total,
            percentage: Math.round((progressEvent.loaded * 100) / progressEvent.total),
          });
        }
      },
      signal,
    },
  );
  return response.data;
}

export async function refreshDirectory(
  cloudAccountUid: string,
  directoryId: string,
): Promise<void> {
  await httpClient.post('/files/refresh', { cloudAccountUid, directoryId });
}

export async function getFolderTree(
  cloudAccountUid: string,
  parentId?: string,
): Promise<PagedResult<CloudDriveFolderDto>> {
  const response = await httpClient.get<PagedResult<CloudDriveFolderDto>>('/files/folders', {
    params: { cloudAccountUid, parentId },
  });
  return response.data;
}
