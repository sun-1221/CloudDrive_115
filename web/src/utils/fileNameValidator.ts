const ILLEGAL_CHARS = /[<>:"/\\|?*\x00-\x1f]/;
const RESERVED_NAMES = new Set([
  'CON', 'PRN', 'AUX', 'NUL',
  'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
  'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9',
]);

export interface FileNameValidationResult {
  valid: boolean;
  reason?: string;
}

export function validateFileName(name: string): FileNameValidationResult {
  if (!name || name.trim().length === 0) {
    return { valid: false, reason: '文件名不能为空' };
  }

  const trimmed = name.trim();

  if (trimmed !== name) {
    return { valid: false, reason: '文件名不能以空格开头或结尾' };
  }

  if (ILLEGAL_CHARS.test(name)) {
    return { valid: false, reason: '文件名包含非法字符' };
  }

  if (name.endsWith('.') || name.endsWith(' ')) {
    return { valid: false, reason: '文件名不能以点或空格结尾' };
  }

  const baseName = name.split('.')[0]?.toUpperCase() ?? '';
  if (RESERVED_NAMES.has(baseName)) {
    return { valid: false, reason: '文件名是系统保留名称' };
  }

  if (name.length > 255) {
    return { valid: false, reason: '文件名长度不能超过 255 个字符' };
  }

  return { valid: true };
}
