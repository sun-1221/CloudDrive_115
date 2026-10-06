import httpClient from './http';
import type { CloudDriveBalanceDto, CloudDriveSpaceInfoDto, CloudDriveVipInfoDto } from '@/types';
export const getVip = async () => (await httpClient.get<CloudDriveVipInfoDto>('/user/vip')).data;
export const getBalance = async () => (await httpClient.get<CloudDriveBalanceDto>('/user/balance')).data;
export const getStorage = async () => (await httpClient.get<CloudDriveSpaceInfoDto>('/user/storage')).data;
