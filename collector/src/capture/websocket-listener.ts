/**
 * WebSocket 监听器 —— MVP 仅记录握手信息和连接生命周期
 */
import type { Page } from 'playwright';
import type { Logger } from 'pino';

export interface WebSocketSummary {
  url: string;
  connectedAt: string;
  closedAt: string | null;
  messageCount: number;
}

export class WebSocketListener {
  private readonly summaries: WebSocketSummary[] = [];

  constructor(private readonly logger: Logger) {}

  async attach(page: Page): Promise<void> {
    page.on('websocket', (ws) => {
      const summary: WebSocketSummary = {
        url: ws.url(),
        connectedAt: new Date().toISOString(),
        closedAt: null,
        messageCount: 0,
      };

      ws.on('framereceived', () => { summary.messageCount++; });
      ws.on('framesent', () => { summary.messageCount++; });
      ws.on('close', () => { summary.closedAt = new Date().toISOString(); });

      this.summaries.push(summary);
      this.logger.debug({ url: ws.url() }, 'websocket connected');
    });
  }

  getSummaries(): WebSocketSummary[] {
    return [...this.summaries];
  }
}
