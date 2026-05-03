/**
 * 域名范围过滤 —— 白名单优先，黑名单作为补充
 */

export class ScopeFilter {
  constructor(
    private readonly allowedDomains: string[],
    private readonly deniedDomains: string[] = [],
  ) {}

  /** 判断指定 hostname 是否在采集范围内 */
  isAllowed(hostname: string): boolean {
    if (this.deniedDomains.some((d) => hostname === d || hostname.endsWith(`.${d}`))) {
      return false;
    }
    return this.allowedDomains.some((d) => hostname === d || hostname.endsWith(`.${d}`));
  }
}
