import { describe, it, expect } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import { sortFiles } from '@/utils/fileSorter';
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

describe('Property 3: 文件列表排序——文件夹优先', () => {
  fcTest.prop([fc.array(fileArbitrary, { minLength: 0, maxLength: 100 })])(
    '任意文件列表排序后，所有文件夹始终出现在普通文件之前',
    (files) => {
      for (const field of ['name', 'size', 'modifiedTime'] as const) {
        for (const order of ['asc', 'desc'] as const) {
          const sorted = sortFiles(files, field, order);

          let lastDirIndex = -1;
          let firstFileIndex = sorted.length;

          sorted.forEach((f, i) => {
            if (f.isDirectory) lastDirIndex = i;
            if (!f.isDirectory && i < firstFileIndex) firstFileIndex = i;
          });

          expect(lastDirIndex).toBeLessThan(firstFileIndex === sorted.length ? sorted.length : firstFileIndex);
        }
      }
    },
  );
});

describe('fileSorter 基础测试', () => {
  it('空列表排序返回空列表', () => {
    expect(sortFiles([], 'name', 'asc')).toEqual([]);
  });

  it('按名称升序排序', () => {
    const files = [
      makeFile({ name: 'b.txt', isDirectory: false }),
      makeFile({ name: 'a.txt', isDirectory: false }),
    ];
    const sorted = sortFiles(files, 'name', 'asc');
    expect(sorted[0]?.name).toBe('a.txt');
    expect(sorted[1]?.name).toBe('b.txt');
  });

  it('文件夹始终排在文件前面', () => {
    const files = [
      makeFile({ name: 'z-file.txt', isDirectory: false }),
      makeFile({ name: 'a-folder', isDirectory: true }),
    ];
    const sorted = sortFiles(files, 'name', 'asc');
    expect(sorted[0]?.isDirectory).toBe(true);
    expect(sorted[1]?.isDirectory).toBe(false);
  });
});
