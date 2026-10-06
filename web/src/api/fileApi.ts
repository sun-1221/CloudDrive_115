import httpClient from './http';
import type {
  FileListResult,
  GetFileListInput,
  CreateFolderInput,
  RenameFileInput,
  CloudDriveOperationResultDto,
  CloudDriveFileDetailDto,
  CloudDriveDownloadDto,
  CopyFilesInput,
} from '@/types';

export async function getFileList(input: GetFileListInput, signal?: AbortSignal): Promise<FileListResult> {
  const response = await httpClient.get<FileListResult>('/files', {
    params: {
      cid: input.directoryId,
      offset: input.offset,
      limit: input.limit,
      type: input.type,
      suffix: input.suffix,
      asc: input.asc,
      o: input.orderBy,
      custom_order: input.customOrder,
      stdir: input.stdir,
      star: input.star,
      showDir: input.showDir,
      natsort: input.natSort,
      snap: input.snap,
      count_folders: input.countFolders,
      record_open_time: input.recordOpenTime,
      code: input.code,
      scid: input.scid,
      source: input.source,
    },
    signal,
  });
  return response.data;
}

export async function getFileDetail(fileId: string): Promise<CloudDriveFileDetailDto> {
  const response = await httpClient.get<CloudDriveFileDetailDto>(`/files/${fileId}`);
  return response.data;
}

export async function createFolder(input: CreateFolderInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/folders', input);
  return response.data;
}

export async function renameFile(input: RenameFileInput): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.patch<CloudDriveOperationResultDto>(
    `/files/${input.fileId}`,
    { newFileName: input.newFileName },
  );
  return response.data;
}

export async function getDownloadUrl(pickCode: string): Promise<CloudDriveDownloadDto> {
  const response = await httpClient.get<CloudDriveDownloadDto>(`/files/${pickCode}/download`);
  return response.data;
}

export async function copySingle(input: CopyFilesInput): Promise<CloudDriveOperationResultDto> {
  if (input.fileIds.length !== 1) throw new Error('单项复制仅支持一个文件');
  const response = await httpClient.post<CloudDriveOperationResultDto>('/files/copy', {
    fileId: input.fileIds[0],
    targetDirectoryId: input.targetDirectoryId,
  });
  return response.data;
}


export async function toggleStar(fileId: string, starred: boolean): Promise<CloudDriveOperationResultDto> {
  const response = await httpClient.post<CloudDriveOperationResultDto>(
    `/files/${fileId}/star`,
    { starred },
  );
  return response.data;
}
