import httpClient from './http';
import type { AuthResponseDto, CloudDriveAuthCheckDto, CurrentUserDto } from '@/types';

export async function checkLoginStatus(): Promise<CloudDriveAuthCheckDto> {
  const response = await httpClient.get<CloudDriveAuthCheckDto>('/auth/check');
  return response.data;
}

export async function register(userName: string, password: string): Promise<AuthResponseDto> {
  const response = await httpClient.post<AuthResponseDto>('/auth/register', { userName, password });
  return response.data;
}

export async function login(userName: string, password: string): Promise<AuthResponseDto> {
  const response = await httpClient.post<AuthResponseDto>('/auth/login', { userName, password });
  return response.data;
}

export async function logout(): Promise<void> {
  await httpClient.post('/auth/logout');
}

export async function refresh(): Promise<AuthResponseDto> {
  const response = await httpClient.post<AuthResponseDto>('/auth/refresh');
  return response.data;
}

export async function getCurrentUser(): Promise<CurrentUserDto> {
  const response = await httpClient.get<CurrentUserDto>('/auth/me');
  return response.data;
}
