import { describe, it, expect } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import { validateFileName } from '@/utils/fileNameValidator';

const ILLEGAL_CHARS = '<>:"/\\|?*';
const ILLEGAL_RE = /[<>:"/\\|?*\x00-\x1f]/;

const RESERVED_NAMES = new Set([
  'CON', 'PRN', 'AUX', 'NUL',
  'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
  'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9',
]);

describe('Property 4: 文件名校验', () => {
  fcTest.prop([fc.string({ minLength: 1, maxLength: 200 }).filter(
    (s: string) => s.trim() === s && s.length > 0 && s.length <= 255 && !ILLEGAL_RE.test(s) && !s.endsWith('.') && !s.endsWith(' '),
  )])(
    '不含非法字符且不以点/空格结尾的非空字符串应通过校验',
    (name: string) => {
      const base = name.split('.')[0]?.toUpperCase() ?? '';
      if (RESERVED_NAMES.has(base)) return;

      const result = validateFileName(name);
      expect(result.valid).toBe(true);
    },
  );

  fcTest.prop([fc.constantFrom(...ILLEGAL_CHARS.split(''))])(
    '包含任意非法字符的文件名应校验失败',
    (illegalChar: string) => {
      const name = `test${illegalChar}file.txt`;
      const result = validateFileName(name);
      expect(result.valid).toBe(false);
      expect(result.reason).toBeDefined();
    },
  );
});

describe('fileNameValidator 基础测试', () => {
  it('空字符串校验失败', () => {
    expect(validateFileName('').valid).toBe(false);
  });

  it('纯空白校验失败', () => {
    expect(validateFileName('   ').valid).toBe(false);
  });

  it('正常文件名校验通过', () => {
    expect(validateFileName('my-file.txt').valid).toBe(true);
  });

  it('超长文件名校验失败', () => {
    expect(validateFileName('a'.repeat(256)).valid).toBe(false);
  });

  it('保留名校验失败', () => {
    expect(validateFileName('CON.txt').valid).toBe(false);
  });

  it('以点结尾校验失败', () => {
    expect(validateFileName('test.').valid).toBe(false);
  });
});
