import { describe, it, expect, beforeEach } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useFileStore } from '@/stores/fileStore';
import type { CloudDriveFileDto } from '@/types';

function makeFile(overrides: Partial<CloudDriveFileDto> = {}): CloudDriveFileDto {
  return {
    fileId: overrides.fileId ?? `file-${Math.random()}`,
    name: overrides.name ?? 'test.txt',
    size: overrides.size ?? 100,
    fileType: overrides.fileType ?? 'txt',
    isDirectory: overrides.isDirectory ?? false,
    modifiedTime: overrides.modifiedTime ?? '2024-01-01T00:00:00Z',
    ...overrides,
  };
}

const fileArbitrary = fc.record({
  fileId: fc.uuid(),
  name: fc.string({ minLength: 1, maxLength: 50 }).filter((s) => !/[\0/\\]/.test(s)),
  size: fc.nat({ max: 1_000_000_000 }),
  fileType: fc.constantFrom('txt', 'jpg', 'mp4', 'pdf', 'zip', 'folder'),
  isDirectory: fc.boolean(),
  modifiedTime: fc.integer({ min: 1577836800000, max: 1893456000000 }).map((ms) => new Date(ms).toISOString()),
}).map((r) => makeFile(r));

beforeEach(() => {
  setActivePinia(createPinia());
});

describe('Property 14: 全选只作用于当前已加载结果', () => {
  fcTest.prop([
    fc.array(fileArbitrary, { minLength: 1, maxLength: 50 }),
    fc.boolean(),
  ])(
    '全选后 selectedFileIds 长度等于 files 长度；hasMore=true 时展示提示',
    (files, hasMore) => {
      const store = useFileStore();
      store.files = files;
      store.hasMore = hasMore;

      store.selectAll();

      // 全选只作用于当前已加载结果
      expect(store.selectedFileIds.length).toBe(files.length);
      expect(store.selectedFileIds).toEqual(files.map((f) => f.fileId));

      // 未完整加载时展示精确提示
      if (hasMore) {
        expect(store.selectionScopeNotice).toBe('已选择当前已加载项目');
      }
    },
  );
});

describe('Property 29: 本地降级排序与筛选必须提示用户', () => {
  fcTest.prop([
    fc.constantFrom('name', 'size', 'modifiedTime') as fc.Arbitrary<'name' | 'size' | 'modifiedTime'>,
    fc.constantFrom('asc', 'desc') as fc.Arbitrary<'asc' | 'desc'>,
  ])(
    '服务端不支持排序时，本地排序必须展示降级提示',
    async (field, order) => {
      const store = useFileStore();
      store.queryCapabilities = { serverSort: false, serverFilter: false };

      await store.setSort(field, order);

      expect(store.sortMode).toBe('local');
      expect(store.selectionScopeNotice).toBe('仅对当前已加载项目生效');
    },
  );

  fcTest.prop([
    fc.constantFrom('image', 'video', 'document', 'audio', 'archive'),
  ])(
    '服务端不支持筛选时，本地筛选必须展示降级提示',
    async (fileType) => {
      const store = useFileStore();
      store.queryCapabilities = { serverSort: false, serverFilter: false };

      await store.setFilter(fileType);

      expect(store.filterMode).toBe('local');
      expect(store.selectionScopeNotice).toBe('仅对当前已加载项目生效');
    },
  );
});

describe('fileStore 导航测试', () => {
  it('moveFocusDown 从无焦点移至第一项', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];
    store.focusedFileId = null;

    store.moveFocusDown();

    expect(store.focusedFileId).toBe('a');
  });

  it('moveFocusUp 在首项时保持不动', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];
    store.focusedFileId = 'a';

    store.moveFocusUp();

    expect(store.focusedFileId).toBe('a');
  });

  it('moveFocusDown 从中间移至下一项', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' }), makeFile({ fileId: 'c' })];
    store.focusedFileId = 'b';

    store.moveFocusDown();

    expect(store.focusedFileId).toBe('c');
  });

  it('moveFocusDown 在末项时保持不动', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];
    store.focusedFileId = 'b';

    store.moveFocusDown();

    expect(store.focusedFileId).toBe('b');
  });
});

describe('fileStore 选择操作', () => {
  it('toggleSelect 单选模式：选择一个文件', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];

    store.toggleSelect('a');

    expect(store.selectedFileIds).toEqual(['a']);
    expect(store.focusedFileId).toBe('a');
  });

  it('toggleSelect 单选模式：切换到另一个文件替换选择', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];

    store.toggleSelect('a');
    store.toggleSelect('b');

    expect(store.selectedFileIds).toEqual(['b']);
  });

  it('toggleSelect Ctrl 模式：追加/移除单项', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' }), makeFile({ fileId: 'c' })];

    store.toggleSelect('a');
    store.toggleSelect('b', true); // Ctrl+click 追加
    expect(store.selectedFileIds).toEqual(['a', 'b']);

    store.toggleSelect('a', true); // Ctrl+click 移除
    expect(store.selectedFileIds).toEqual(['b']);
  });

  it('toggleSelect Shift 模式：范围选择', () => {
    const store = useFileStore();
    store.files = [
      makeFile({ fileId: 'a' }),
      makeFile({ fileId: 'b' }),
      makeFile({ fileId: 'c' }),
      makeFile({ fileId: 'd' }),
    ];

    store.toggleSelect('a');          // 聚焦 a
    store.toggleSelect('c', false, true); // Shift+click c → 选中 a,b,c

    expect(store.selectedFileIds).toEqual(['a', 'b', 'c']);
  });

  it('clearSelection 清空所有选择', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' })];
    store.toggleSelect('a');
    expect(store.selectedFileIds.length).toBe(1);

    store.clearSelection();

    expect(store.selectedFileIds).toEqual([]);
    expect(store.selectionScopeNotice).toBeNull();
  });

  it('selectAll 后 isAllSelected 为 true', () => {
    const store = useFileStore();
    store.files = [makeFile({ fileId: 'a' }), makeFile({ fileId: 'b' })];
    store.hasMore = false;

    store.selectAll();

    expect(store.isAllSelected).toBe(true);
  });
});

describe('fileStore 视图模式', () => {
  it('setViewMode 切换列表/网格', () => {
    const store = useFileStore();
    expect(store.viewMode).toBe('list');

    store.setViewMode('grid');
    expect(store.viewMode).toBe('grid');

    store.setViewMode('list');
    expect(store.viewMode).toBe('list');
  });
});
