import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { ApiProblemDetails } from '@/types';
import { parseProblemDetails } from './problemDetails';
import { disableCapability, type CloudDriveCapability } from '@/capabilities/registry';

let tokenExpiredHandler: ((problem?: ApiProblemDetails) => void) | null = null;
let serviceUnavailableHandler: ((reason: string) => void) | null = null;
let problemDetailsHandler: ((problem: ApiProblemDetails) => void) | null = null;

export function setTokenExpiredHandler(handler: (problem?: ApiProblemDetails) => void): void {
  tokenExpiredHandler = handler;
}

export function setServiceUnavailableHandler(handler: (reason: string) => void): void {
  serviceUnavailableHandler = handler;
}

export function setProblemDetailsHandler(handler: (problem: ApiProblemDetails) => void): void {
  problemDetailsHandler = handler;
}

const httpClient: AxiosInstance = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

type RetriableRequestConfig = InternalAxiosRequestConfig & { _authRetry?: boolean };
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const currentToken = localStorage.getItem('115web.accessToken');
  if (!currentToken) return null;
  if (!refreshPromise) {
    refreshPromise = axios.post<{ accessToken?: string }>('/api/auth/refresh', undefined, {
      headers: { Authorization: `Bearer ${currentToken}` },
      timeout: 30000,
    }).then(({ data }) => {
      const nextToken = typeof data.accessToken === 'string' ? data.accessToken : null;
      if (nextToken) localStorage.setItem('115web.accessToken', nextToken);
      return nextToken;
    }).catch(() => null).finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

// 请求拦截器
httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('115web.accessToken');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  },
);

// 响应拦截器
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiProblemDetails>) => {
    if (axios.isCancel(error)) {
      // 被取消的请求不触发全局错误
      return Promise.reject(error);
    }

    const status = error.response?.status;
    const problem = parseProblemDetails(error);

    const config = error.config as RetriableRequestConfig | undefined;
    const requestUrl = config?.url ?? '';
    if (status === 401 && config && !config._authRetry && !requestUrl.includes('/auth/login') && !requestUrl.includes('/auth/register') && !requestUrl.includes('/auth/refresh')) {
      const nextToken = await refreshAccessToken();
      if (nextToken) {
        config._authRetry = true;
        config.headers.Authorization = `Bearer ${nextToken}`;
        return httpClient.request(config);
      }
    }

    if (problem) {
      problemDetailsHandler?.(problem);
      if (problem.code === 'cloud_drive_api_not_collected') {
        const path = error.config?.url ?? '';
        const capability: CloudDriveCapability | undefined =
          path.includes('/search') ? 'search' :
          path.includes('/upload') ? 'upload' :
          path.includes('/move') ? 'move' :
          path.includes('/recycle') && error.config?.method !== 'get' ? 'recycleWrite' :
          path.includes('/copy') ? 'copySingle' :
          path.includes('/star') ? 'star' :
          path.includes('/folders') ? 'createFolder' :
          path.includes('/download') ? 'download' :
          path.includes('/files/') && error.config?.method === 'patch' ? 'rename' : undefined;
        if (capability) disableCapability(capability);
      }
    }

    if (status === 401) {
      // Cloud Drive 令牌失效
      tokenExpiredHandler?.(problem ?? undefined);
      return Promise.reject(error);
    }

    if ((status === 502 || status === 503) && problem?.detail) {
      // 功能不可用
      serviceUnavailableHandler?.(problem.detail);
      return Promise.reject(error);
    }

    return Promise.reject(error);
  },
);

export function extractProblemDetails(error: unknown): ApiProblemDetails | null {
  return parseProblemDetails(error);
}

export { parseProblemDetails, sanitizeForTelemetry, toSafeUiError } from './problemDetails';

export function isNetworkError(error: unknown): boolean {
  return axios.isAxiosError(error) && !error.response;
}

export function isCancelledError(error: unknown): boolean {
  return axios.isCancel(error);
}

export default httpClient;
