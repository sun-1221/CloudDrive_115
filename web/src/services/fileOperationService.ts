import * as fileApi from '@/api/fileApi';
import { isCapabilityEnabled } from '@/capabilities/registry';
import { executeOperation, type OperationResult } from './operationService';
import type {
  CloudDriveDownloadDto,
  CloudDriveOperationResultDto,
} from '@/types';
import { validateFileName } from '@/utils/fileNameValidator';

function assertEnabled(capability: Parameters<typeof isCapabilityEnabled>[0]): void {
  if (!isCapabilityEnabled(capability)) {
    throw new Error('当前操作暂不可用');
  }
}

export async function download(pickCode: string): Promise<CloudDriveDownloadDto> {
  assertEnabled('download');
  return fileApi.getDownloadUrl(pickCode);
}

export async function createFolder(parentId: string, folderName: string): Promise<OperationResult<CloudDriveOperationResultDto>> {
  assertEnabled('createFolder');
  const validation = validateFileName(folderName);
  if (!validation.valid) throw new Error(validation.reason ?? '文件名无效');
  return executeOperation({ execute: () => fileApi.createFolder({ parentId, folderName }) });
}

export async function copySingle(fileId: string, targetDirectoryId: string): Promise<OperationResult<CloudDriveOperationResultDto>> {
  assertEnabled('copySingle');
  return executeOperation({ execute: () => fileApi.copySingle({ fileIds: [fileId], targetDirectoryId }) });
}

export async function rename(fileId: string, newName: string): Promise<OperationResult<CloudDriveOperationResultDto>> {
  assertEnabled('rename');
  const validation = validateFileName(newName);
  if (!validation.valid) throw new Error(validation.reason ?? '文件名无效');
  return executeOperation({ execute: () => fileApi.renameFile({ fileId, newFileName: newName }) });
}

export async function toggleStar(fileId: string, starred: boolean): Promise<OperationResult<CloudDriveOperationResultDto>> {
  assertEnabled('star');
  return executeOperation({ execute: () => fileApi.toggleStar(fileId, starred) });
}
