import { describe, it, expect, vi } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import { fetchWithRetry } from '@/services/temporaryResourceService';

describe('Property 5: 临时资源地址自动重试一次', () => {
  fcTest.prop([fc.webUrl()])(
    '首次失败时自动重试一次并返回成功',
    async (url: string) => {
      let callCount = 0;
      const result = await fetchWithRetry({
        fetchUrl: async () => {
          callCount++;
          if (callCount === 1) throw new Error('expired');
          return { url };
        },
        extractUrl: (data) => data.url,
      });

      expect(result.success).toBe(true);
      expect(result.url).toBe(url);
      expect(callCount).toBe(2);
    },
  );

  fcTest.prop([fc.string({ minLength: 1 })])(
    '两次都失败时返回失败状态和错误信息',
    async (errorMsg: string) => {
      let callCount = 0;
      const result = await fetchWithRetry({
        fetchUrl: async () => {
          callCount++;
          throw new Error(errorMsg);
        },
        extractUrl: (data) => data as string,
      });

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(callCount).toBe(2); // 最多重试一次
    },
  );
});

describe('temporaryResourceService 基础测试', () => {
  it('首次成功直接返回', async () => {
    const result = await fetchWithRetry({
      fetchUrl: async () => ({ url: 'https://example.com/file.jpg' }),
      extractUrl: (data) => data.url,
    });

    expect(result.success).toBe(true);
    expect(result.url).toBe('https://example.com/file.jpg');
  });

  it('首次失败触发 onFirstFailure 回调', async () => {
    const onFirstFailure = vi.fn();
    let callCount = 0;

    await fetchWithRetry({
      fetchUrl: async () => {
        callCount++;
        if (callCount === 1) throw new Error('expired');
        return { url: 'https://example.com/new-url.jpg' };
      },
      extractUrl: (data) => data.url,
      onFirstFailure,
    });

    expect(onFirstFailure).toHaveBeenCalledOnce();
  });
});
