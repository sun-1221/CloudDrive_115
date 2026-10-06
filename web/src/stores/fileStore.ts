import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as fileApi from '@/api/fileApi';
import { sortFiles, filterFilesByType } from '@/utils/fileSorter';
import type {
  CloudDriveFileDto,
  BreadcrumbItem,
  CloudDrivePathDto,
  FileQueryCapabilities,
} from '@/types';

const ROOT_DIRECTORY_ID = '0';
const PAGE_SIZE = 50;

export const useFileStore = defineStore('file', () => {
  const currentDirectoryId = ref(ROOT_DIRECTORY_ID);
  const breadcrumbs = ref<BreadcrumbItem[]>([{ id: ROOT_DIRECTORY_ID, name: '根目录' }]);
  const paths = ref<CloudDrivePathDto[]>([]);
  const files = ref<CloudDriveFileDto[]>([]);
  const totalCount = ref(0);
  const queryCapabilities = ref<FileQueryCapabilities>({
    serverSort: false,
    serverFilter: false,
  });
  const offset = ref(0);
  const hasMore = ref(false);
  const viewMode = ref<'list' | 'grid'>('list');
  const sortField = ref<'name' | 'size' | 'modifiedTime'>('name');
  const sortOrder = ref<'asc' | 'desc'>('asc');
  const sortMode = ref<'server' | 'local'>('local');
  const fileTypeFilter = ref('');
  const filterMode = ref<'server' | 'local'>('local');
  const selectedFileIds = ref<string[]>([]);
  const selectionScopeNotice = ref<string | null>(null);
  const focusedFileId = ref<string | null>(null);
  const loading = ref(false);
  const loadingMore = ref(false);
  const error = ref<string | null>(null);

  // 请求去重
  let fetchAbortController: AbortController | null = null;
  let fetchMoreDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  let requestToken = 0;

  const selectedFiles = computed(() =>
    files.value.filter((f) => selectedFileIds.value.includes(f.fileId)),
  );

  const imageFiles = computed(() =>
    files.value.filter((f) => {
      if (f.isDirectory) return false;
      const ext = f.name.split('.').pop()?.toLowerCase() ?? '';
      const previewable = new Set(['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp']);
      return previewable.has(ext);
    }),
  );

  const isAllSelected = computed(() =>
    files.value.length > 0 && selectedFileIds.value.length === files.value.length,
  );

  const displayedFiles = computed(() => {
    let result = files.value;

    if (fileTypeFilter.value && filterMode.value === 'local') {
      result = filterFilesByType(result, fileTypeFilter.value);
    }

    if (sortMode.value === 'local') {
      result = sortFiles(result, sortField.value, sortOrder.value);
    }

    return result;
  });

  async function fetchFiles(directoryId?: string): Promise<void> {
    // 取消之前的请求
    if (fetchAbortController) {
      fetchAbortController.abort();
    }
    fetchAbortController = new AbortController();
    const token = ++requestToken;

    const targetDirId = directoryId ?? currentDirectoryId.value ?? ROOT_DIRECTORY_ID;
    currentDirectoryId.value = targetDirId;

    loading.value = true;
    error.value = null;
    offset.value = 0;

    try {
      const result = await fileApi.getFileList({
        directoryId: targetDirId,
        offset: 0,
        limit: PAGE_SIZE,
      }, fetchAbortController.signal);

      // AbortController 不能阻止所有 mock/缓存实现返回；令牌是第二道竞态保护。
      if (token !== requestToken) return;

      files.value = result.items;
      totalCount.value = result.totalCount;
      offset.value = result.items.length;
      hasMore.value = result.items.length < result.totalCount;
      paths.value = result.paths ?? [];
      if (result.paths && result.paths.length > 0) {
        breadcrumbs.value = result.paths.map((path) => ({ id: path.fileId, name: path.fileName }));
      }
      selectedFileIds.value = [];
      selectionScopeNotice.value = null;
      focusedFileId.value = null;
    } catch (e) {
      if (token !== requestToken || (e instanceof DOMException && e.name === 'AbortError')) return;
      error.value = e instanceof Error ? e.message : '加载文件列表失败';
    } finally {
      if (token === requestToken) loading.value = false;
    }
  }

  async function fetchMore(): Promise<void> {
    if (!hasMore.value || loadingMore.value) return;

    // 防抖去重
    if (fetchMoreDebounceTimer) {
      clearTimeout(fetchMoreDebounceTimer);
    }

    return new Promise((resolve) => {
      fetchMoreDebounceTimer = setTimeout(async () => {
        fetchMoreDebounceTimer = null;
        loadingMore.value = true;

        try {
          const token = requestToken;
          const result = await fileApi.getFileList({
            directoryId: currentDirectoryId.value,
            offset: offset.value,
            limit: PAGE_SIZE,
          });

          if (token !== requestToken) return;

          files.value = [...files.value, ...result.items];
          offset.value += result.items.length;
          hasMore.value = offset.value < result.totalCount;
        } catch (e) {
          if (e instanceof DOMException && e.name === 'AbortError') return;
          // 加载更多失败不覆盖主错误
        } finally {
          loadingMore.value = false;
        }

        resolve();
      }, 200);
    });
  }

  async function refreshDirectory(): Promise<void> {
    const directoryId = currentDirectoryId.value;
    await fetchFiles(directoryId);
  }

  async function navigateTo(directoryId: string, breadcrumbName?: string): Promise<void> {
    const normalizedId = directoryId || ROOT_DIRECTORY_ID;

    if (breadcrumbName) {
      // 检查是否在面包屑中已有该项（用于回退导航）
      const existingIndex = breadcrumbs.value.findIndex((b) => b.id === normalizedId);
      if (existingIndex >= 0) {
        breadcrumbs.value = breadcrumbs.value.slice(0, existingIndex + 1);
      } else {
        breadcrumbs.value = [...breadcrumbs.value, { id: normalizedId, name: breadcrumbName }];
      }
    }

    await fetchFiles(normalizedId);
  }

  async function navigateUp(): Promise<void> {
    if (breadcrumbs.value.length <= 1) return;
    breadcrumbs.value = breadcrumbs.value.slice(0, -1);
    const parentDir = breadcrumbs.value[breadcrumbs.value.length - 1];
    if (parentDir) {
      await fetchFiles(parentDir.id);
    }
  }

  function setViewMode(mode: 'list' | 'grid'): void {
    viewMode.value = mode;
  }

  async function setSort(field: 'name' | 'size' | 'modifiedTime', order: 'asc' | 'desc'): Promise<void> {
    sortField.value = field;
    sortOrder.value = order;

    if (!queryCapabilities.value.serverSort) {
      sortMode.value = 'local';
      selectionScopeNotice.value = '仅对当前已加载项目生效';
    }
  }

  async function setFilter(fileType: string): Promise<void> {
    fileTypeFilter.value = fileType;

    if (!queryCapabilities.value.serverFilter) {
      filterMode.value = 'local';
      if (fileType) {
        selectionScopeNotice.value = '仅对当前已加载项目生效';
      }
    }
  }

  function toggleSelect(fileId: string, ctrlKey: boolean = false, shiftKey: boolean = false): void {
    if (shiftKey && focusedFileId.value) {
      // Shift 范围选择
      const currentFiles = displayedFiles.value;
      const startIdx = currentFiles.findIndex((f) => f.fileId === focusedFileId.value);
      const endIdx = currentFiles.findIndex((f) => f.fileId === fileId);

      if (startIdx >= 0 && endIdx >= 0) {
        const min = Math.min(startIdx, endIdx);
        const max = Math.max(startIdx, endIdx);
        const rangeIds = currentFiles.slice(min, max + 1).map((f) => f.fileId);

        if (ctrlKey) {
          // Ctrl+Shift 追加范围
          const uniqueIds = new Set([...selectedFileIds.value, ...rangeIds]);
          selectedFileIds.value = [...uniqueIds];
        } else {
          selectedFileIds.value = rangeIds;
        }
      }
    } else if (ctrlKey) {
      // Ctrl 切换单项
      const index = selectedFileIds.value.indexOf(fileId);
      if (index >= 0) {
        selectedFileIds.value = selectedFileIds.value.filter((id) => id !== fileId);
      } else {
        selectedFileIds.value = [...selectedFileIds.value, fileId];
      }
    } else {
      // 单选
      selectedFileIds.value = [fileId];
    }

    focusedFileId.value = fileId;
  }

  function selectAll(): void {
    selectedFileIds.value = files.value.map((f) => f.fileId);
    if (hasMore.value) {
      selectionScopeNotice.value = '已选择当前已加载项目';
    }
  }

  function acknowledgeSelectionScopeNotice(): void {
    selectionScopeNotice.value = null;
  }

  function focusFile(fileId: string): void {
    focusedFileId.value = fileId;
  }

  function moveFocusDown(): void {
    const current = displayedFiles.value;
    if (current.length === 0) return;
    const idx = current.findIndex((f) => f.fileId === focusedFileId.value);
    const nextIdx = idx < 0 ? 0 : Math.min(idx + 1, current.length - 1);
    focusedFileId.value = current[nextIdx]?.fileId ?? null;
  }

  function moveFocusUp(): void {
    const current = displayedFiles.value;
    if (current.length === 0) return;
    const idx = current.findIndex((f) => f.fileId === focusedFileId.value);
    const prevIdx = idx <= 0 ? 0 : idx - 1;
    focusedFileId.value = current[prevIdx]?.fileId ?? null;
  }

  function clearSelection(): void {
    selectedFileIds.value = [];
    selectionScopeNotice.value = null;
  }

  return {
    currentDirectoryId,
    breadcrumbs,
    paths,
    files,
    totalCount,
    queryCapabilities,
    offset,
    hasMore,
    viewMode,
    sortField,
    sortOrder,
    sortMode,
    fileTypeFilter,
    filterMode,
    selectedFileIds,
    selectionScopeNotice,
    focusedFileId,
    loading,
    loadingMore,
    error,
    selectedFiles,
    imageFiles,
    isAllSelected,
    displayedFiles,
    fetchFiles,
    fetchMore,
    refreshDirectory,
    navigateTo,
    navigateUp,
    setViewMode,
    setSort,
    setFilter,
    toggleSelect,
    selectAll,
    acknowledgeSelectionScopeNotice,
    focusFile,
    moveFocusDown,
    moveFocusUp,
    clearSelection,
  };
});
