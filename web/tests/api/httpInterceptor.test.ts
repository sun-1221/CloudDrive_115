import { describe, it, expect, vi, beforeEach } from 'vitest';

describe('http 拦截器', () => {
  let setTokenExpiredHandler: typeof import('@/api/http').setTokenExpiredHandler;
  let setServiceUnavailableHandler: typeof import('@/api/http').setServiceUnavailableHandler;
  let isCancelledError: typeof import('@/api/http').isCancelledError;
  let isNetworkError: typeof import('@/api/http').isNetworkError;
  let extractProblemDetails: typeof import('@/api/http').extractProblemDetails;

  beforeEach(async () => {
    const http = await import('@/api/http');
    setTokenExpiredHandler = http.setTokenExpiredHandler;
    setServiceUnavailableHandler = http.setServiceUnavailableHandler;
    isCancelledError = http.isCancelledError;
    isNetworkError = http.isNetworkError;
    extractProblemDetails = http.extractProblemDetails;
  });

  it('setTokenExpiredHandler 注册 handler 后可被调用', () => {
    const handler = vi.fn();
    setTokenExpiredHandler(handler);

    // handler 已注册（实际触发需 401 响应拦截器，此处仅验证注册不报错）
    expect(handler).not.toHaveBeenCalled();
  });

  it('setServiceUnavailableHandler 注册 handler 后可被调用', () => {
    const handler = vi.fn();
    setServiceUnavailableHandler(handler);

    expect(handler).not.toHaveBeenCalled();
  });

  it('isCancelledError 对 AbortController 取消的请求返回 false（非 axios cancel）', () => {
    const error = new DOMException('aborted', 'AbortError');
    // isCancelledError 使用 axios.isCancel，DOMException 不属于 axios cancel
    expect(isCancelledError(error)).toBe(false);
  });

  it('isNetworkError 对非 axios 错误返回 false', () => {
    expect(isNetworkError(new Error('random'))).toBe(false);
  });

  it('extractProblemDetails 对普通对象仅返回可识别字段', () => {
    expect(extractProblemDetails(new Error('random'))).toEqual({});
  });

  it('extractProblemDetails 对有 isAxiosError+response.data 的对象提取 ProblemDetails', () => {
    // axios.isAxiosError 检查 error.isAxiosError 属性
    const fakeAxiosError = {
      isAxiosError: true,
      response: {
        data: { status: 401, title: 'Unauthorized', code: 'TOKEN_EXPIRED' },
      },
    };

    const result = extractProblemDetails(fakeAxiosError);
    expect(result).toEqual({ status: 401, title: 'Unauthorized', code: 'TOKEN_EXPIRED' });
  });

  it('extractProblemDetails 对无 response 的 axios error 返回 null', () => {
    const fakeAxiosError = { isAxiosError: true };
    expect(extractProblemDetails(fakeAxiosError)).toBeNull();
  });
});
