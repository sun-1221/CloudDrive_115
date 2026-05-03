# 115-cloud-drive-integration Specs 审核报告（第 2 轮 — 修复验证）

## 审核快照

- **审核时间**：2026-04-18 12:31:11
- **审核轮次**：**第 2 轮**（修复验证）
- **官方文档基线**：`e:\Code\115web\docs\115-api-docs.md`（2026-04-08 采集，`runId: 20260408145821-oqg9xv`，未变更）
- **Specs 基线**：`e:\Code\115web\.kiro\specs\115-cloud-drive-integration\requirements.md`（605 行，第 1 轮修复后）
- **上轮审核**：`audit-history/2026-04-18-121351.md` — 首次审核发现 **22 项问题**
- **本轮范围**：验证第 1 轮 22 项修复是否到位 + 回归检查（无需重做全量矩阵）

## 审核工作量

- 上轮遗留问题：22 项全部检查
- 本轮范围：逐项修复验证（每项附 spec 修复位置）
- 回归检查：验证新增内容未破坏原有需求结构

---

## 修复状态矩阵

### 🔴 高优先级（8 项全部修复）

| 编号 | 问题 | 上轮状态 | 本轮状态 | Spec 修复位置 | 验证结论 |
|------|------|:-------:|:-------:|--------------|---------|
| H-01 | `/files/search` 错误码 `errCode` | ❌ | ✅ | `requirements.md:459` 错误变体表第 2 行 | 已单独列为第 2 行，使用 `errCode`（number） |
| H-02 | `/movies/subtitle` 错误码 `errNo` 大写 | ❌ | ✅ | `requirements.md:461` 错误变体表第 4 行 | 已单独列为第 4 行，标注"大写 N" |
| H-03 | `user/vip_limit`、`index_info` 的 `code` 类型 | ❌ | ✅ | `requirements.md:465` 错误变体表第 8 行 | 标注 `code（string）`，明确区分 proapi 的 `code（number）` |
| H-04 | `pay.115.com/` 错误变体表遗漏 | ❌ | ✅ | `requirements.md:468` 错误变体表第 11 行 | 新增第 11 行，使用 `msg`（非 `message`） |
| H-05 | `/web/lixian/*` 漏 `ct=lixian` | ❌ | ✅ | `requirements.md:365, 369, 370` | 三端点均补充 `?ct=lixian&ac=xxx` |
| H-06 | `/files/search` 幻影参数 `gte_day/lte_day` | ❌ | ✅ | `requirements.md:125` | 替换为 `date`，并标注开放平台参数不采用 |
| H-07 | `/files/video` 漏 `share_id` | ❌ | ✅ | `requirements.md:302` | 已加入 `&share_id=0` 及说明 |
| H-08 | `/files` 漏 `aid=1` 必填参数 | ❌ | ✅ | `requirements.md:109` | 已补充，并说明回收站为 `aid=7` |

### 🟠 中高优先级（4 项全部修复）

| 编号 | 问题 | 上轮状态 | 本轮状态 | Spec 修复位置 | 验证结论 |
|------|------|:-------:|:-------:|--------------|---------|
| M-01 | 用户敏感字段黑名单不完整 | ❌ | ✅ | `requirements.md:416-425, 491-499` | 扩展至 **8 类 34 个字段**（联系方式/支付/住址/生日/人口/职业/兴趣/其他隐私）；需求 23 与需求 26 黑名单同步 |
| M-02 | `/rb` 漏 `aid=7/cid=0/format` | ❌ | ✅ | `requirements.md:269` | 已补充三个必填参数 |
| M-03 | `/share/*` 漏 `format=json` | ❌ | ✅ | `requirements.md:395-397` | 三端点均补充 `&format=json` |
| M-04 | `task_lists` Body 漏 `stat` | ❌ | ✅ | `requirements.md:365-366` | Body 参数补充为 `{ page, stat, uid, sign, time }`，并说明 `stat=0` 默认全部 |

### 🟡 中优先级（7 项全部修复）

| 编号 | 问题 | 上轮状态 | 本轮状态 | Spec 修复位置 | 验证结论 |
|------|------|:-------:|:-------:|--------------|---------|
| L-01 | `limit=1150` 无来源 | ❌ | ✅ | `requirements.md:31, 110` | 简介新增"硬上限来源说明"；需求 4.2 标注"保护限值来自开放平台文档推断" |
| L-02 | `offset+limit≤10000` 无来源 | ❌ | ✅ | `requirements.md:31, 126` | 同 L-01 |
| L-03 | 回收站动态键说明不精准 | ❌ | ✅ | `requirements.md:270-271` | 明确说明"有数据时 `data` 为对象，键为文件 ID (string)"、"空回收站时官方不返回 `data` 键" |
| L-04 | `task_count` 字段映射未细化 | ❌ | ✅ | `requirements.md:370` | 补充 `total_count→total`、`failed_count→failed`、`finished_count→completed`、`downloading_count→active` |
| L-05 | `get_quota_package_info` DTO 缺失 | ❌ | ✅ | `requirements.md:369` | 新增 `CloudDriveOfflineQuotaDto { package, count, surplus, maxSize, used }` |
| L-06 | 13 个官方未使用 API 无排除声明 | ❌ | ✅ | `requirements.md:19-29` | 简介新增"官方 API 范围排除声明"，显式列出 13 个端点与排除原因 |
| L-07 | `sta/hdf/class/fvs` 值域映射无依据 | ❌ | ✅ | `requirements.md:112, 368` | 需求 4.4 标注"基于开放平台文档推断，以真实响应为准验证"；需求 20.5 同步 |

### 🟢 低优先级（3 项全部修复）

| 编号 | 问题 | 上轮状态 | 本轮状态 | Spec 修复位置 | 验证结论 |
|------|------|:-------:|:-------:|--------------|---------|
| S-01 | Cookie 冷却时间无默认值 | ❌ | ✅ | `requirements.md:74` | 新增"默认 60 秒，可通过 `CookieCooldownSeconds` 配置" |
| S-02 | `vip.115.com/` DTO 说明缺失 | ❌ | ✅ | `requirements.md:426` | 新增完整字段结构 `{ num, url, vip_count, space_count, forever_count, sqcode_count, sqcode_cancel_count, 00_balance }`，并说明 `00_balance` 索引访问归一化为 `balance` |
| S-03 | `share/send/receive` 双 errNo/errno 处理策略 | ❌ | ✅ | `requirements.md:464` | 错误变体表第 7 行明确"优先读 `errNo`，fallback `errno`" |

---

## 回归检查

### 结构完整性

- **需求条目**：1-30 全部保留（含 13a 星标子需求），无缺失 ✅
- **非功能性需求**：性能/安全/可观测性/部署/测试覆盖 5 个子节完整 ✅
- **术语表**：14 个术语完整 ✅
- **前置要求与约束**：5 条保留 ✅

### 语义一致性

- **敏感字段黑名单同步**：需求 23.4（应用层过滤）与需求 26.1（日志层过滤）黑名单完全一致，避免两处定义不同步 ✅
- **错误变体表 vs 实际错误字段**：12 行表格与 17 个端点的官方采集响应逐一核对，无遗漏 ✅
- **命名约定**：所有 DTO 使用 `CloudDrive*Dto` 前缀（`CloudDriveOfflineTaskDto`、`CloudDriveOfflineTaskListDto`、`CloudDriveOfflineQuotaDto`、`CloudDriveUserInfoDto` 等），风格统一 ✅

### 新增内容质量

- **简介"官方 API 范围排除声明"**：13 个端点逐一列出并说明排除原因，精确对应 `docs/115-api-docs.md` 中的 B01-B13 ✅
- **简介"硬上限来源说明"**：明确标注限值来自开放平台文档推断，需以真实响应为准 ✅
- **错误变体表"指针"注解**：补充 `ErrorFieldAdapter` 注册表设计思路，为 design.md 阶段提供落地指引 ✅
- **需求 26.1 黑名单与需求 23.4 同步**：通过"黑名单与需求 23 同步"注解强制一致性 ✅

### 未引入新问题

- 所有修复未破坏原有需求的验收标准（acceptance criteria）结构
- 所有修复保持 EARS 语法（WHEN/IF/THE ... SHALL ...）规范
- 所有修复保留了原有的"待补采集"状态标记

---

## 覆盖率验证

- **上轮问题总数**：22 项
- **本轮已修复**：**22 / 22 = 100%** ✅
- **新发现问题**：0 项
- **遗留问题**：0 项
- **回归问题**：0 项

---

## 下轮审核建议

本 requirements.md 已达到**可进入 design.md 阶段**的质量要求。后续审核重点：

1. **design.md 阶段**：
   - 审核 DTO 字段与 requirements 归一化映射表一致性（需求 4.4、14.4、20.4、23.3 等）
   - 审核 `ErrorFieldAdapter` 注册表实现是否覆盖错误变体表 12 行
   - 审核 `CloudDriveSensitiveFieldsFilter`（或等价组件）是否覆盖 34 个敏感字段
   - 审核 Gateway 层签名生成是否包含 `page/stat/uid/sign/time` 五项 Body 参数

2. **tasks.md 阶段**：
   - 审核任务颗粒度是否覆盖 30 条需求的所有验收标准
   - 审核"待补采集"项目的任务是否预留占位

3. **实现阶段**：
   - 真实 Cookie 场景联调时验证以下开放平台推断项：
     - `sta/hdf/class/fvs` 字段值域（需求 4.4）
     - 云下载 `status` 值域（需求 20.5）
     - `limit=1150`、`offset+limit≤10000` 实际硬上限（需求 4.2、5.3）
     - 云下载签名算法（需求 20.3）

---

## 修复工作量实际统计

| 优先级 | 预估 | 实际修改行数 | 状态 |
|-------|-----|------------|------|
| 🔴 高 | ~30 行 | 约 18 行编辑 + 简介新增约 18 行 | ✅ |
| 🟠 中高 | ~20 行 | 约 14 行编辑 + 敏感字段扩展约 18 行 | ✅ |
| 🟡 中 | ~20 行 | 简介新增约 13 行 + 分散编辑约 10 行 | ✅ |
| 🟢 低 | ~10 行 | 约 5 行编辑 | ✅ |
| **合计** | **~80 行** | **requirements.md 从 568 行增至 605 行（+37 行净增长）** | **100% 完成** |
