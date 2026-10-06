import httpClient from './http';
import type { CloudDriveAccountSummaryDto } from '@/types';

export async function getAccounts(): Promise<CloudDriveAccountSummaryDto[]> {
  const response = await httpClient.get<CloudDriveAccountSummaryDto[]>('/cloud-drive/accounts');
  return response.data;
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
