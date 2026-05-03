/**
 * DOM 探测 —— 用于定位失败时输出诊断信息
 */
import type { Page } from 'playwright';

export interface ProbeResult {
  selector: string;
  found: boolean;
  candidateCount: number;
  candidates: string[];
}

export class DomProbe {
  /** 探测指定选择器，返回匹配情况 */
  async probe(page: Page, selector: string): Promise<ProbeResult> {
    const elements = await page.locator(selector).all();
    const candidates: string[] = [];

    for (const el of elements.slice(0, 5)) {
      const tag = await el.evaluate((e) => e.tagName.toLowerCase());
      const text = (await el.textContent())?.trim().slice(0, 50) ?? '';
      candidates.push(`<${tag}> ${text}`);
    }

    return {
      selector,
      found: elements.length > 0,
      candidateCount: elements.length,
      candidates,
    };
  }
}
