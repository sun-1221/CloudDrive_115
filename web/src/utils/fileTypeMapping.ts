import type { CloudDriveFileDto, FileCategory } from '@/types';

export const extensionCategoryMap: Record<string, FileCategory> = {
  jpg: 'image', jpeg: 'image', png: 'image', gif: 'image', bmp: 'image', webp: 'image', svg: 'image',
  mp4: 'video', mkv: 'video', avi: 'video', mov: 'video', wmv: 'video', flv: 'video', webm: 'video',
  pdf: 'document', doc: 'document', docx: 'document', xls: 'document', xlsx: 'document',
  ppt: 'document', pptx: 'document', txt: 'document', md: 'document',
  mp3: 'audio', wav: 'audio', flac: 'audio', aac: 'audio', ogg: 'audio', wma: 'audio',
  zip: 'archive', rar: 'archive', '7z': 'archive', tar: 'archive', gz: 'archive',
};

export const previewableImageExtensions = new Set([
  'jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp',
]);

export function getFileExtension(fileName: string): string {
  return fileName.split('.').pop()?.toLowerCase() ?? '';
}

export function getFileCategory(file: CloudDriveFileDto): FileCategory {
  if (file.isDirectory) return 'folder';
  const ext = getFileExtension(file.name);
  return extensionCategoryMap[ext] ?? 'other';
}

export function isPreviewableImage(file: CloudDriveFileDto): boolean {
  if (file.isDirectory) return false;
  const ext = getFileExtension(file.name);
  return previewableImageExtensions.has(ext);
}

const categoryIconMap: Record<FileCategory, string> = {
  folder: 'Folder',
  image: 'Image',
  video: 'Video',
  document: 'FileText',
  audio: 'Music',
  archive: 'Archive',
  other: 'File',
};

const categoryColorMap: Record<FileCategory, string> = {
  folder: 'var(--color-folder)',
  image: 'var(--color-image)',
  video: 'var(--color-video)',
  document: 'var(--color-document)',
  audio: 'var(--color-audio)',
  archive: 'var(--color-archive)',
  other: 'var(--color-other)',
};

export function getFileIcon(category: FileCategory): string {
  return categoryIconMap[category];
}

export function getFileColor(category: FileCategory): string {
  return categoryColorMap[category];
}
