const SIZE_UNITS = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];

export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  if (bytes < 0) return '未知大小';

  const k = 1024;
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  const index = Math.min(i, SIZE_UNITS.length - 1);
  const size = bytes / Math.pow(k, index);

  return `${size.toFixed(index === 0 ? 0 : 1)} ${SIZE_UNITS[index]}`;
}

export function formatDate(dateStr: string, format: 'full' | 'short' | 'relative' = 'full'): string {
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '未知日期';

  if (format === 'relative') {
    return _formatRelativeDate(date);
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  if (format === 'short') {
    return `${year}-${month}-${day}`;
  }

  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day} ${hours}:${minutes}`;
}

export function formatRelativeDate(dateOrStr: string | Date): string {
  const date = typeof dateOrStr === 'string' ? new Date(dateOrStr) : dateOrStr;
  if (isNaN(date.getTime())) return '未知日期';
  return _formatRelativeDate(date);
}

function _formatRelativeDate(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffSeconds < 60) return '刚刚';
  if (diffMinutes < 60) return `${diffMinutes} 分钟前`;
  if (diffHours < 24) return `${diffHours} 小时前`;
  if (diffDays < 7) return `${diffDays} 天前`;
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} 周前`;

  return formatDate(date.toISOString(), 'short');
}

export function formatDuration(seconds: number): string {
  if (seconds < 0 || !isFinite(seconds)) return '00:00';

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);

  if (hours > 0) {
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
  }
  return `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

export function formatProgress(loaded: number, total: number): string {
  if (total <= 0) return '0%';
  const percentage = Math.min(100, Math.round((loaded / total) * 100));
  return `${percentage}%`;
}

export function truncateFileName(name: string, maxLength: number = 40): string {
  if (name.length <= maxLength) return name;

  const dotIndex = name.lastIndexOf('.');
  if (dotIndex === -1) {
    return name.substring(0, maxLength - 3) + '...';
  }

  const ext = name.substring(dotIndex);
  const baseName = name.substring(0, dotIndex);
  const maxBase = maxLength - ext.length - 3;

  if (maxBase <= 0) return name.substring(0, maxLength - 3) + '...';
  return baseName.substring(0, maxBase) + '...' + ext;
}
