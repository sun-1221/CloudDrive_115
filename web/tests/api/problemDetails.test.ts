import { describe, expect, it } from 'vitest';
import { parseProblemDetails, sanitizeForTelemetry, toSafeUiError } from '@/api/problemDetails';

describe('ProblemDetails 安全边界', () => {
  it('优先读取根级 code，并兼容 extensions.code', () => {
    expect(parseProblemDetails({ detail: '输入无效', extensions: { code: 'bad_input' } })).toMatchObject({
      code: 'bad_input',
      detail: '输入无效',
    });
  });

  it('400 errors 优先转成字段级安全提示', () => {
    const result = toSafeUiError({ status: 400, errors: { name: ['名称格式不正确'] } });
    expect(result.message).toBe('名称格式不正确');
  });

  it('not-collected 使用稳定提示且不暴露上游消息', () => {
    const result = toSafeUiError({ status: 503, code: 'cloud_drive_api_not_collected', detail: 'upstreamMessage' });
    expect(result.message).toBe('该功能暂未采集，当前不可用');
    expect(result.message).not.toContain('upstreamMessage');
  });

  it('脱敏会移除敏感 key 和临时 URL', () => {
    const result = sanitizeForTelemetry({ fileName: 'secret.txt', pickCode: 'p', amount: 10, detail: 'https://temp.invalid/a' });
    expect(result).toEqual({ detail: '[已隐藏]' });
  });
});
