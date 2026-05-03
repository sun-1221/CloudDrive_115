/**
 * 响应暂存 —— 缓存响应体文本，避免重复读取
 */
export class ResponseStore {
  private readonly cache = new Map<string, string | null>();

  set(networkId: string, body: string | null): void {
    this.cache.set(networkId, body);
  }

  get(networkId: string): string | null | undefined {
    return this.cache.get(networkId);
  }

  remove(networkId: string): void {
    this.cache.delete(networkId);
  }

  clear(): void {
    this.cache.clear();
  }
}
