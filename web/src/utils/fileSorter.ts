import type { CloudDriveFileDto } from '@/types';

export type SortField = 'name' | 'size' | 'modifiedTime';
export type SortOrder = 'asc' | 'desc';

export function sortFiles(
  files: CloudDriveFileDto[],
  field: SortField,
  order: SortOrder,
): CloudDriveFileDto[] {
  const sorted = [...files];

  sorted.sort((a, b) => {
    // 文件夹始终优先于普通文件
    if (a.isDirectory !== b.isDirectory) {
      return a.isDirectory ? -1 : 1;
    }

    let comparison = 0;

    switch (field) {
      case 'name':
        comparison = a.name.localeCompare(b.name, 'zh-CN', { numeric: true });
        break;
      case 'size':
        comparison = a.size - b.size;
        break;
      case 'modifiedTime':
        comparison = new Date(a.modifiedTime).getTime() - new Date(b.modifiedTime).getTime();
        break;
    }

    return order === 'asc' ? comparison : -comparison;
  });

  return sorted;
}

export function filterFilesByType(
  files: CloudDriveFileDto[],
  fileType: string,
): CloudDriveFileDto[] {
  if (!fileType) return files;

  return files.filter((file) => {
    if (file.isDirectory) return true; // 文件夹始终保留
    return file.fileType.toLowerCase() === fileType.toLowerCase();
  });
}
