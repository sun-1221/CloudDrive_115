import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// Mock fileApi.upload — 同步完成模式
const mockUpload = vi.fn().mockResolvedValue({ accepted: false, jobId: null });

vi.mock('@/api/fileApi', () => ({
  upload: (...args: unknown[]) => mockUpload(...args),
  refreshDirectory: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('@/stores/jobStore', () => ({
  useJobStore: () => ({ addJob: vi.fn() }),
}));

vi.mock('@/stores/fileStore', () => ({
  useFileStore: () => ({ currentDirectoryId: '0', refreshDirectory: vi.fn() }),
}));

beforeEach(() => {
  mockUpload.mockClear();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('Property 19: 多文件上传逐文件建模', () => {
  it.each([1, 3, 5, 8])(
    '上传 %i 个文件时，每个文件独立调用 upload',
    async (fileCount) => {
      const { enqueue } = await import('@/services/uploadService');

      const files: File[] = [];
      for (let i = 0; i < fileCount; i++) {
        files.push(new File(['x'], `batch-${Date.now()}-${i}.txt`, { type: 'text/plain' }));
      }

      // enqueue 内部对每个文件独立调用 processUpload
      await enqueue(files, 'uid-test', 'dir-0');

      // 等待所有异步上传微任务完成
      await vi.advanceTimersByTimeAsync(100);

      // 每个文件独立一次 upload 调用
      expect(mockUpload).toHaveBeenCalledTimes(fileCount);

      // 每次调用的第一个参数是不同的 File 对象
      const uploadedFiles = mockUpload.mock.calls.map((c: unknown[]) => (c[0] as File).name);
      const uniqueNames = new Set(uploadedFiles);
      expect(uniqueNames.size).toBe(fileCount);
    },
  );
});
