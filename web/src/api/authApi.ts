import httpClient from './http';
import type {
  ExchangeCodeInput,
  CloudDrivePkceAuthorizationDto,
  CloudDriveTokenDto,
  CloudDriveAccountSummaryDto,
  CloudDriveUserInfoDto,
} from '@/types';
import { normalizeFlowType } from '@/utils/enumNormalizers';

export async function getAuthorizationUrl(): Promise<string> {
  const response = await httpClient.get<string>('/auth/authorize');
  return response.data;
}

export async function createPkceSession(): Promise<CloudDrivePkceAuthorizationDto> {
  const response = await httpClient.post<CloudDrivePkceAuthorizationDto>('/auth/pkce/session');
  return response.data;
}

export async function exchangeCode(input: ExchangeCodeInput): Promise<CloudDriveTokenDto> {
  const payload = {
    ...input,
    flowType: input.flowType
      ? (typeof input.flowType === 'number' ? normalizeFlowType(input.flowType) : input.flowType)
      : undefined,
  };
  const response = await httpClient.post<CloudDriveTokenDto>('/auth/callback', payload);
  return response.data;
}

export async function listAccounts(): Promise<CloudDriveAccountSummaryDto[]> {
  const response = await httpClient.get<CloudDriveAccountSummaryDto[]>('/auth/accounts');
  return response.data;
}

export async function getUserInfo(cloudAccountUid: string): Promise<CloudDriveUserInfoDto> {
  const response = await httpClient.get<CloudDriveUserInfoDto>('/auth/user-info', {
    params: { cloudAccountUid },
  });
  return response.data;
}

export async function disconnect(cloudAccountUid: string): Promise<void> {
  await httpClient.delete('/auth/disconnect', {
    params: { cloudAccountUid },
  });
}
