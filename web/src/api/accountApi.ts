import httpClient from './http';
import type { CloudDriveAccountSummaryDto } from '@/types';

export async function getAccounts(): Promise<CloudDriveAccountSummaryDto[]> {
  const response = await httpClient.get<CloudDriveAccountSummaryDto[]>('/cloud-drive/accounts');
  // 当前后端返回数组；保留对标准 list envelope 的兼容，避免把协议兼容逻辑扩散到页面层。
  if (Array.isArray(response.data)) return response.data;
  const envelope = response.data as unknown as { items?: CloudDriveAccountSummaryDto[] };
  return Array.isArray(envelope?.items) ? envelope.items : [];
}

export async function bindAccount(input: { cloudAccountUid: string; uid: string; cid: string; ussid: string }): Promise<void> {
  await httpClient.post('/cloud-drive/accounts', input);
}

export async function selectAccount(cloudAccountUid: string): Promise<void> {
  await httpClient.post('/cloud-drive/accounts/select', { cloudAccountUid });
}

export async function unbindAccount(cloudAccountUid: string): Promise<void> {
  await httpClient.delete(`/cloud-drive/accounts/${encodeURIComponent(cloudAccountUid)}`);
}
