import { describe, it, expect, vi } from 'vitest';

const mockUpload = vi.fn();
vi.mock('@/api/fileApi', () => ({
  upload: (...args: unknown[]) => mockUpload(...args),
}));

describe('上传能力 disabled 契约', () => {
  it.each([1, 3, 5, 8])(
    '上传 %i 个文件时不调用上传 API，并将每个文件标记为不可用',
    async (fileCount) => {
      const { enqueue, getQueue, clearFinished } = await import('@/services/uploadService');
      clearFinished();

      const files: File[] = [];
      for (let i = 0; i < fileCount; i++) {
        files.push(new File(['x'], `batch-${Date.now()}-${i}.txt`, { type: 'text/plain' }));
      }

      await enqueue(files, 'dir-0');

      expect(mockUpload).not.toHaveBeenCalled();
      expect(getQueue().slice(-fileCount)).toEqual(
        expect.arrayContaining(files.map((file) => expect.objectContaining({
          fileName: file.name,
          directoryId: 'dir-0',
          status: 'failed',
          errorMessage: '上传功能暂未采集，当前不可用',
        }))),
      );
    },
  );
});
