import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { ApiProblemDetails } from '@/types';

let tokenExpiredHandler: ((problem?: ApiProblemDetails) => void) | null = null;
let serviceUnavailableHandler: ((reason: string) => void) | null = null;

export function setTokenExpiredHandler(handler: (problem?: ApiProblemDetails) => void): void {
  tokenExpiredHandler = handler;
}

export function setServiceUnavailableHandler(handler: (reason: string) => void): void {
  serviceUnavailableHandler = handler;
}

const httpClient: AxiosInstance = axios.create({
  baseURL: '/api/cloud-drive',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// 请求拦截器
httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  },
);

// 响应拦截器
httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiProblemDetails>) => {
    if (axios.isCancel(error)) {
      // 被取消的请求不触发全局错误
      return Promise.reject(error);
    }

    const status = error.response?.status;
    const problem = error.response?.data;

    if (status === 401 && problem?.code) {
      // Cloud Drive 令牌失效
      tokenExpiredHandler?.(problem);
      return Promise.reject(error);
    }

    if (status === 503 && problem?.detail) {
      // 功能不可用
      serviceUnavailableHandler?.(problem.detail);
      return Promise.reject(error);
    }

    return Promise.reject(error);
  },
);

export function extractProblemDetails(error: unknown): ApiProblemDetails | null {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as Record<string, unknown>;
    if (typeof data === 'object' && ('status' in data || 'title' in data || 'code' in data)) {
      return data as ApiProblemDetails;
    }
  }
  return null;
}

export function isNetworkError(error: unknown): boolean {
  return axios.isAxiosError(error) && !error.response;
}

export function isCancelledError(error: unknown): boolean {
  return axios.isCancel(error);
}

export default httpClient;
