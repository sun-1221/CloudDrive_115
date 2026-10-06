export interface FetchWithRetryOptions<T> {
  fetchUrl: () => Promise<T>;
  extractUrl: (result: T) => string;
  onFirstFailure?: () => void;
}

export interface FetchWithRetryResult<T> {
  success: boolean;
  data?: T;
  url?: string;
  error?: string;
}

/** 组件级临时资源容器；不写入 Pinia 或浏览器持久化存储。 */
export function createTemporaryResourceScope() {
  let value = '';
  return {
    set(next: string) { value = next; },
    get() { return value; },
    clear() { value = ''; },
  };
}

export async function fetchWithRetry<T>(
  options: FetchWithRetryOptions<T>,
): Promise<FetchWithRetryResult<T>> {
  try {
    const data = await options.fetchUrl();
    const url = options.extractUrl(data);
    if (!url) throw new Error('资源地址不可用');
    return { success: true, data, url };
  } catch {
    options.onFirstFailure?.();

    // 自动重试一次
    try {
      const data = await options.fetchUrl();
      const url = options.extractUrl(data);
      if (!url) throw new Error('资源地址不可用');
      return { success: true, data, url };
    } catch (secondError) {
      const errorMessage =
        secondError instanceof Error ? secondError.message : '资源加载失败，请重试';
      return { success: false, error: errorMessage };
    }
  }
}
