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

export async function fetchWithRetry<T>(
  options: FetchWithRetryOptions<T>,
): Promise<FetchWithRetryResult<T>> {
  try {
    const data = await options.fetchUrl();
    const url = options.extractUrl(data);
    return { success: true, data, url };
  } catch (firstError) {
    options.onFirstFailure?.();

    // 自动重试一次
    try {
      const data = await options.fetchUrl();
      const url = options.extractUrl(data);
      return { success: true, data, url };
    } catch (secondError) {
      const errorMessage =
        secondError instanceof Error ? secondError.message : '资源加载失败，请重试';
      return { success: false, error: errorMessage };
    }
  }
}
