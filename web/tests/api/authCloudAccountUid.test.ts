import { describe, expect, vi, beforeEach } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import httpClient from '@/api/http';

// 拦截 httpClient 请求，记录参数
let lastGetParams: Record<string, unknown> = {};
let lastPostData: Record<string, unknown> = {};

beforeEach(() => {
  lastGetParams = {};
  lastPostData = {};
  vi.restoreAllMocks();
});

function mockGet(data: unknown) {
  vi.spyOn(httpClient, 'get').mockImplementation(async (_url, config) => {
    lastGetParams = (config?.params as Record<string, unknown>) ?? {};
    return { data, status: 200, statusText: 'OK', headers: {}, config: {} as any };
  });
}

function mockPost(data: unknown) {
  vi.spyOn(httpClient, 'post').mockImplementation(async (_url, body) => {
    lastPostData = (body as Record<string, unknown>) ?? {};
    return { data, status: 200, statusText: 'OK', headers: {}, config: {} as any };
  });
}

describe('Property 1: 授权后请求必须携带 CloudAccountUid', () => {
  fcTest.prop([fc.uuid(), fc.uuid()])(
    'getFileList 请求参数包含 cloudAccountUid',
    async (uid, dirId) => {
      mockGet({ items: [], totalCount: 0, queryCapabilities: { serverSort: false, serverFilter: false } });
      const { getFileList } = await import('@/api/fileApi');

      await getFileList({
        cloudAccountUid: uid,
        directoryId: dirId,
        offset: 0,
        limit: 10,
      });

      expect(lastGetParams.cloudAccountUid).toBe(uid);
    },
  );

  fcTest.prop([fc.uuid(), fc.uuid()])(
    'getFileDetail 请求参数包含 cloudAccountUid',
    async (uid, fileId) => {
      mockGet({ fileId, name: 'test.txt', size: 0, isDirectory: false });
      const { getFileDetail } = await import('@/api/fileApi');

      await getFileDetail(uid, fileId);

      expect(lastGetParams.cloudAccountUid).toBe(uid);
    },
  );

  fcTest.prop([fc.uuid(), fc.uuid()])(
    'getImagePreview 请求参数包含 cloudAccountUid',
    async (uid, fileId) => {
      mockGet({ previewUrl: 'https://example.com/img.jpg', isTemporary: true });
      const { getImagePreview } = await import('@/api/previewApi');

      await getImagePreview(uid, fileId);

      expect(lastGetParams.cloudAccountUid).toBe(uid);
    },
  );

  fcTest.prop([fc.uuid(), fc.uuid()])(
    'getPlayUrl 请求体包含 cloudAccountUid',
    async (uid, fileId) => {
      mockPost({ status: 'Ready', qualityOptions: [], duration: 0, isTemporary: false });
      const { getPlayUrl } = await import('@/api/videoApi');

      await getPlayUrl({ cloudAccountUid: uid, fileId });

      expect(lastPostData.cloudAccountUid).toBe(uid);
    },
  );
});
