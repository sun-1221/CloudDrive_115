/**
 * SQLite 索引存储 —— 派生分析索引，可从原始记录重建
 *
 * 注意：需要安装 better-sqlite3（原生模块，需 C++ 编译工具链）
 *   npm install better-sqlite3 @types/better-sqlite3
 */
import type { CaptureRecord } from '../model/capture-record.js';

/** better-sqlite3 的最小类型定义（避免未安装时编译失败） */
interface SqliteDatabase {
  pragma(source: string): unknown;
  exec(source: string): void;
  prepare(source: string): { run(...params: unknown[]): unknown };
  close(): void;
}

export class SqliteStore {
  private db: SqliteDatabase | null = null;

  constructor(private readonly dbPath: string) {}

  /** 初始化数据库并创建表结构 */
  async open(): Promise<void> {
    // 动态导入，运行时才需要 better-sqlite3
    const { default: Database } = await import('better-sqlite3' as string);
    this.db = new Database(this.dbPath) as SqliteDatabase;
    this.db.pragma('journal_mode = WAL');
    this.createTables();
  }

  private createTables(): void {
    this.getDb().exec(`
      CREATE TABLE IF NOT EXISTS capture_records (
        id TEXT PRIMARY KEY,
        run_id TEXT NOT NULL,
        scenario_id TEXT NOT NULL,
        method TEXT NOT NULL,
        url TEXT NOT NULL,
        pathname TEXT NOT NULL,
        status INTEGER,
        content_type TEXT,
        duration_ms INTEGER,
        from_cache INTEGER DEFAULT 0,
        captured_at TEXT NOT NULL
      );

      CREATE INDEX IF NOT EXISTS idx_records_run ON capture_records(run_id);
      CREATE INDEX IF NOT EXISTS idx_records_scenario ON capture_records(scenario_id);
      CREATE INDEX IF NOT EXISTS idx_records_pathname ON capture_records(pathname);
    `);
  }

  /** 插入一条采集记录的索引 */
  insertRecord(record: CaptureRecord): void {
    const stmt = this.getDb().prepare(`
      INSERT OR REPLACE INTO capture_records
      (id, run_id, scenario_id, method, url, pathname, status, content_type, duration_ms, from_cache, captured_at)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `);

    stmt.run(
      record.id, record.runId, record.scenarioId,
      record.method, record.url, record.pathname,
      record.status, record.contentType, record.durationMs,
      record.fromCache ? 1 : 0, record.capturedAt,
    );
  }

  /** 关闭数据库 */
  close(): void {
    if (this.db) {
      this.db.close();
      this.db = null;
    }
  }

  private getDb(): SqliteDatabase {
    if (!this.db) throw new Error('SqliteStore 尚未打开');
    return this.db;
  }
}
