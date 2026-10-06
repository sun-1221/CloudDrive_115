import { describe, expect, vi, beforeEach } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import httpClient from '@/api/http';

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('浏览器请求使用服务端会话，不传入 CloudAccountUid', () => {
  fcTest.prop([fc.uuid()])('getFileList 传目录参数但不传账号身份', async (dirId) => {
    const get = vi.spyOn(httpClient, 'get').mockResolvedValue({ data: { items: [], totalCount: 0 } });
    const { getFileList } = await import('@/api/fileApi');
    await getFileList({ directoryId: dirId, offset: 0, limit: 10 });
    expect(get).toHaveBeenLastCalledWith('/files', expect.objectContaining({ params: expect.objectContaining({ cid: dirId, offset: 0, limit: 10 }) }));
    expect(get.mock.calls.at(-1)?.[1]?.params).not.toHaveProperty('cloudAccountUid');
  });

  fcTest.prop([fc.uuid()])('getFileDetail 以 fileId 定位详情', async (fileId) => {
    const get = vi.spyOn(httpClient, 'get').mockResolvedValue({ data: { fileId, name: 'test.txt', size: 0, isDirectory: false } });
    const { getFileDetail } = await import('@/api/fileApi');
    await getFileDetail(fileId);
    expect(get).toHaveBeenLastCalledWith(`/files/${fileId}`);
  });

  fcTest.prop([fc.uuid()])('getImagePreview 以 pickCode 定位预览', async (pickCode) => {
    const get = vi.spyOn(httpClient, 'get').mockResolvedValue({ data: { previewUrl: 'https://example.com/img.jpg', isTemporary: true } });
    const { getImagePreview } = await import('@/api/previewApi');
    await getImagePreview(pickCode);
    expect(get).toHaveBeenLastCalledWith(`/files/${pickCode}/preview`);
  });

  fcTest.prop([fc.uuid()])('getPlayUrl 为只读 GET，使用 pickCode 而非账号 UID', async (pickCode) => {
    const get = vi.spyOn(httpClient, 'get').mockResolvedValue({ data: { status: 'Ready', qualityOptions: [], duration: 0, isTemporary: false } });
    const { getPlayUrl } = await import('@/api/videoApi');
    await getPlayUrl({ pickCode });
    expect(get).toHaveBeenLastCalledWith(`/videos/${pickCode}/play`, { params: { definition: undefined } });
    expect(get.mock.calls.at(-1)?.[1]?.params).not.toHaveProperty('cloudAccountUid');
  });
});
