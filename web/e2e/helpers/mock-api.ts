import { type Page, type Route } from '@playwright/test';

/**
 * 通用 API Mock 帮助函数
 * 用于拦截后端请求，返回预设数据，使 E2E 测试不依赖后端服务
 *
 * 注意：使用 URL 函数匹配而非 glob 字符串，
 * 因为 glob 的 `?` 匹配单字符而非 query string 分隔符。
 */

// Cloud Drive 前端 API 通过统一 `/api` baseURL 访问，资源路径从 `/files` 等开始。
const BASE = '/api';

/** 判断 URL pathname 是否匹配指定路径（忽略 query string） */
function matchPath(url: string, path: string): boolean {
  try {
    const u = new URL(url);
    return u.pathname === `${BASE}${path}`;
  } catch {
    return url.includes(`${BASE}${path}`);
  }
}

/** 判断 URL pathname 是否以指定前缀开头 */
function matchPathPrefix(url: string, prefix: string): boolean {
  try {
    const u = new URL(url);
    return u.pathname.startsWith(`${BASE}${prefix}`);
  } catch {
    return url.includes(`${BASE}${prefix}`);
  }
}

function json(route: Route, data: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(data),
  });
}

/** 模拟已授权状态：账号列表 + 用户信息 */
export async function mockAuthorizedState(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('115web.accessToken', 'header.payload.signature');
  });
  await page.route(
    (url) => matchPath(url.toString(), '/auth/me'),
    (route) => json(route, {
      userId: 'user-e2e',
      userName: 'e2e-user',
      currentCloudAccountUid: 'e2e-account',
      accounts: [{
        cloudAccountUid: 'e2e-account',
        status: 0,
        creationTime: '2024-06-01T10:00:00Z',
        lastModificationTime: '2024-06-01T10:00:00Z',
      }],
    }),
  );
  await page.route(
    (url) => matchPath(url.toString(), '/auth/check'),
    (route) => json(route, {
      isAuthenticated: true,
      isLoggedIn: true,
      lastCheckedAt: '2024-06-10T10:00:00Z',
    }),
  );
}

/** 模拟未授权状态 */
export async function mockUnauthorizedState(page: Page) {
  await page.addInitScript(() => {
    localStorage.removeItem('115web.accessToken');
  });
  await page.route(
    (url) => matchPath(url.toString(), '/auth/check'),
    (route) => json(route, {
      isAuthenticated: false,
      isLoggedIn: false,
      lastCheckedAt: '2024-06-10T10:00:00Z',
    }),
  );
}

/** 模拟功能不可用（503） */
export async function mockFeatureDisabled(page: Page) {
  await page.addInitScript(() => {
    localStorage.removeItem('115web.accessToken');
  });
  await page.route(
    (url) => matchPath(url.toString(), '/auth/check'),
    (route) => json(route, {
      status: 503,
      title: 'Service Unavailable',
      detail: '115 网盘功能未启用，请先在系统设置中配置',
      code: 'FEATURE_DISABLED',
    }, 503),
  );
}

const defaultFiles = [
  { fileId: 'dir-1', name: '我的文档', size: 0, fileType: 'folder', isDirectory: true, modifiedTime: '2024-06-01T10:00:00Z', thumbnailUrl: '' },
  { fileId: 'dir-2', name: '照片', size: 0, fileType: 'folder', isDirectory: true, modifiedTime: '2024-05-20T08:30:00Z', thumbnailUrl: '' },
  { fileId: 'file-1', name: '报告.pdf', size: 2048576, fileType: 'pdf', isDirectory: false, modifiedTime: '2024-06-10T14:20:00Z', thumbnailUrl: '' },
  { fileId: 'file-2', name: '风景.jpg', size: 5242880, fileType: 'jpg', isDirectory: false, modifiedTime: '2024-06-09T09:15:00Z', thumbnailUrl: 'https://example.com/thumb/landscape.jpg' },
  { fileId: 'file-3', name: '演示.mp4', size: 104857600, fileType: 'mp4', isDirectory: false, modifiedTime: '2024-06-08T16:45:00Z', thumbnailUrl: 'https://example.com/thumb/demo.jpg' },
  { fileId: 'file-4', name: '数据.xlsx', size: 32768, fileType: 'xlsx', isDirectory: false, modifiedTime: '2024-06-07T11:00:00Z', thumbnailUrl: '' },
  { fileId: 'file-5', name: '备忘录.txt', size: 1024, fileType: 'txt', isDirectory: false, modifiedTime: '2024-06-06T20:30:00Z', thumbnailUrl: '' },
];

/** 模拟文件列表（根目录） */
export async function mockFileList(page: Page, files?: unknown[]) {
  const items = files ?? defaultFiles;

  // 精确匹配 /files（带或不带 query string），但不匹配 /files/folder 等子路径
  await page.route(
    (url) => matchPath(url.toString(), '/files'),
    (route) => json(route, {
      items,
      totalCount: items.length,
      queryCapabilities: { serverSort: false, serverFilter: false },
    }),
  );
}

/** 模拟空目录 */
export async function mockEmptyDirectory(page: Page) {
  await mockFileList(page, []);
}

/** 模拟文件操作成功（移动/复制/删除等） */
export async function mockFileOperations(page: Page) {
  const ops = [
    { path: '/files/folder', data: { fileId: 'new-folder-1', name: '新建文件夹' } },
    { path: '/files/rename', data: { success: true } },
    { path: '/files/delete', data: { success: true, accepted: false } },
    { path: '/files/move', data: { success: true, accepted: false } },
    { path: '/files/copy', data: { success: true, accepted: false } },
  ];

  for (const op of ops) {
    await page.route(
      (url) => matchPath(url.toString(), op.path),
      (route) => json(route, op.data),
    );
  }
}

/** 模拟任务列表（空） */
export async function mockJobList(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/jobs'),
    (route) => json(route, { items: [], totalCount: 0 }),
  );
}

/** 模拟目录刷新 */
export async function mockRefreshDirectory(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/files/refresh'),
    (route) => json(route, {}),
  );
}

/** 模拟搜索接口 */
export async function mockSearch(page: Page, results?: unknown[]) {
  const items = results ?? [
    { fileId: 'search-1', name: '搜索结果文件.pdf', size: 1024, fileType: 'pdf', isDirectory: false, modifiedTime: '2024-06-01T10:00:00Z', path: '/文档', thumbnailUrl: '' },
    { fileId: 'search-2', name: '搜索结果图片.jpg', size: 2048, fileType: 'jpg', isDirectory: false, modifiedTime: '2024-06-02T10:00:00Z', path: '/照片', thumbnailUrl: '' },
  ];

  await page.route(
    (url) => matchPath(url.toString(), '/files/search'),
    (route) => json(route, { items, totalCount: items.length }),
  );
}

/** 模拟空搜索结果 */
export async function mockEmptySearch(page: Page) {
  await mockSearch(page, []);
}

/** 模拟回收站列表 */
export async function mockRecycleBinList(page: Page, items?: unknown[]) {
  const defaultItems = [
    { id: 'rb-1', fileName: '已删文件.txt', fileSize: '100 B', type: 'txt', dtime: '2024-06-01', parentName: '/文档', status: 'deleted' },
    { id: 'rb-2', fileName: '已删照片.jpg', fileSize: '5 KB', type: 'jpg', dtime: '2024-06-02', parentName: '/照片', status: 'deleted' },
  ];

  await page.route(
    (url) => matchPath(url.toString(), '/recycle') && !url.toString().includes('/restore') && !url.toString().includes('/permanent-delete') && !url.toString().includes('/clear'),
    (route) => {
      if (route.request().method() === 'GET') {
        return json(route, { items: items ?? defaultItems, totalCount: (items ?? defaultItems).length });
      }
      return route.continue();
    },
  );
}

/** 模拟回收站操作（还原/彻底删除/清空） */
export async function mockRecycleBinOperations(page: Page) {
  await page.route(
    (url) => matchPath(url.toString(), '/recycle-bin/restore'),
    (route) => json(route, { accepted: false, message: '还原成功' }),
  );

  await page.route(
    (url) => matchPath(url.toString(), '/recycle-bin/permanent-delete'),
    (route) => json(route, { accepted: false, message: '彻底删除成功' }),
  );

  await page.route(
    (url) => matchPath(url.toString(), '/recycle-bin/clear'),
    (route) => json(route, { accepted: true, jobId: 'job-clear-rb' }),
  );
}

/** 模拟图片预览 */
export async function mockImagePreview(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/files/') && url.toString().includes('/preview/image'),
    (route) => json(route, {
      previewUrl: 'https://example.com/preview/image.jpg',
      fileName: '风景.jpg',
      fileType: 'jpg',
      isTemporary: true,
    }),
  );
}

/** 模拟视频播放 */
export async function mockVideoPlay(page: Page) {
  await page.route(
    (url) => matchPath(url.toString(), '/video/play'),
    (route) => json(route, {
      status: 'Ready',
      qualityOptions: [
        { quality: '720p', playUrl: 'https://example.com/video/720p.m3u8' },
        { quality: '1080p', playUrl: 'https://example.com/video/1080p.m3u8' },
      ],
      duration: 120,
      isTemporary: true,
    }),
  );

  await page.route(
    (url) => matchPathPrefix(url.toString(), '/video/') && url.toString().includes('/subtitles'),
    (route) => json(route, { items: [] }),
  );
}

/** 模拟文档预览 */
export async function mockDocPreview(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/files/') && url.toString().includes('/preview/document'),
    (route) => json(route, {
      previewUrl: 'https://example.com/preview/doc.pdf',
      fileName: '报告.pdf',
      fileType: 'pdf',
      renderMode: 'Direct',
      isTemporary: true,
    }),
  );
}

/** 模拟文件下载 */
export async function mockFileDownload(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/files/') && url.toString().includes('/download'),
    (route) => json(route, {
      downloadUrl: 'https://example.com/download/file.zip',
      fileName: 'file.zip',
      fileSize: 1024000,
      isTemporary: true,
    }),
  );
}

/** 模拟文件详情 */
export async function mockFileDetail(page: Page) {
  await page.route(
    (url) => {
      const u = new URL(url.toString());
      // 匹配 /files/{fileId} 但不匹配 /files/search, /files/folder 等
      return /^\/api\/files\/[^/]+$/.test(u.pathname) &&
        !u.pathname.endsWith('/search') &&
        !u.pathname.endsWith('/folder') &&
        !u.pathname.endsWith('/rename') &&
        !u.pathname.endsWith('/delete') &&
        !u.pathname.endsWith('/move') &&
        !u.pathname.endsWith('/copy') &&
        !u.pathname.endsWith('/upload') &&
        !u.pathname.endsWith('/refresh') &&
        !u.pathname.endsWith('/folders') &&
        !u.pathname.includes('/preview') &&
        !u.pathname.includes('/download');
    },
    (route) => json(route, {
      fileId: 'file-1',
      name: '报告.pdf',
      size: 2048576,
      fileType: 'pdf',
      isDirectory: false,
      modifiedTime: '2024-06-10T14:20:00Z',
      createdTime: '2024-06-01T10:00:00Z',
      sha1: 'abc123def456',
      parentId: '0',
      path: '/报告.pdf',
    }),
  );
}

/** 模拟目录树（FolderPicker） */
export async function mockFolderTree(page: Page) {
  await page.route(
    (url) => matchPath(url.toString(), '/files/folders'),
    (route) => json(route, {
      items: [
        { folderId: 'dir-1', name: '我的文档', parentId: '0' },
        { folderId: 'dir-2', name: '照片', parentId: '0' },
      ],
      totalCount: 2,
    }),
  );
}

/** 模拟有运行中任务的任务列表 */
export async function mockRunningJobs(page: Page) {
  await page.route(
    (url) => matchPathPrefix(url.toString(), '/jobs'),
    (route) => json(route, {
      items: [
        { id: 'job-1', jobType: 'BatchCopy', status: 'Running', cloudAccountUid: 'test-uid-001', progress: 50, startTime: '2024-06-10T10:00:00Z' },
        { id: 'job-2', jobType: 'Upload', status: 'Completed', cloudAccountUid: 'test-uid-001', progress: 100, startTime: '2024-06-10T09:00:00Z', endTime: '2024-06-10T09:05:00Z' },
      ],
      totalCount: 2,
    }),
  );
}

/** 一键设置全部 Mock（已授权 + 文件列表 + 操作 + 任务） */
export async function setupFullMock(page: Page) {
  // 注意顺序：子路径 mock 先注册，避免被 /files 通配吞掉
  await mockAuthorizedState(page);
  await mockFileOperations(page);
  await mockRefreshDirectory(page);
  await mockJobList(page);
  await mockFileList(page);
}

/** 扩展版全量 Mock（包含搜索、回收站、预览、下载、详情、目录树） */
export async function setupExtendedMock(page: Page) {
  await mockAuthorizedState(page);
  await mockImagePreview(page);
  await mockVideoPlay(page);
  await mockDocPreview(page);
  await mockFileDownload(page);
  await mockFileDetail(page);
  await mockFolderTree(page);
  await mockSearch(page);
  await mockRecycleBinList(page);
  await mockRecycleBinOperations(page);
  await mockFileOperations(page);
  await mockRefreshDirectory(page);
  await mockJobList(page);
  await mockFileList(page);
}
