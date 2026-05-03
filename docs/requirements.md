# 115 网盘 Web 客户端 — 产品需求文档

> 基于 115 开放平台 API（30 个）+ 网页端流量采集（65 个接口 / 15 个域名）梳理

---

## 一、产品定位

重写 115 网盘网页端，提供现代化的文件管理、视频播放、云下载、分享等核心功能。
采用纯前端 SPA 架构，通过开发代理或 BFF 层转发请求到 115 官方 API。

---

## 二、功能模块总览

| 模块 | 优先级 | 对应 API 域名 | 说明 |
|---|---|---|---|
| **M1 登录认证** | P0 | 115.com | Cookie 登录态复用 |
| **M2 文件管理** | P0 | webapi.115.com, 115.com | 浏览、搜索、新建、重命名、移动、复制、删除、下载 |
| **M3 文件上传** | P1 | 开放平台 | 秒传 + 断点续传 + 阿里云 OSS |
| **M4 视频播放** | P1 | webapi.115.com | 在线播放、字幕、进度记忆、转码 |
| **M5 云下载** | P1 | 115.com/web/lixian/ | 链接任务、BT 任务、配额、任务管理 |
| **M6 回收站** | P2 | webapi.115.com | 列表、还原、彻底删除、清空 |
| **M7 分享管理** | P2 | webapi.115.com | 我的分享、发送/接收记录 |
| **M8 用户中心** | P2 | my/vip/pay.115.com | 个人信息、VIP 状态、余额、空间用量 |
| **M9 消息通知** | P3 | msg/home.115.com | 未读消息、通知列表 |

---

## 三、页面规划

### 3.1 全局布局

```
┌─────────────────────────────────────────────────┐
│  顶栏：Logo | 搜索框 | 空间用量 | 用户头像     │
├────────┬────────────────────────────────────────┤
│        │  面包屑导航 / 工具栏                    │
│ 侧边栏  │──────────────────────────────────────│
│        │                                        │
│ · 文件  │           内容区                       │
│ · 视频  │     (列表/网格/详情)                   │
│ · 云下载 │                                       │
│ · 回收站 │                                       │
│ · 分享  │                                        │
│        │                                        │
│ · 标签  │                                        │
│ · 设置  │                                        │
├────────┴────────────────────────────────────────┤
│  状态栏：选中数 | 上传进度 | 网络状态            │
└─────────────────────────────────────────────────┘
```

### 3.2 页面清单

| 页面 | 路由 | 核心功能 | 模块 |
|---|---|---|---|
| **文件浏览** | `/files/:cid?` | 文件列表（列表/网格切换）、排序、筛选、翻页 | M2 |
| **文件搜索** | `/search?q=&type=` | 关键词搜索、类型筛选、时间范围 | M2 |
| **视频播放** | `/video/:pickcode` | HLS 播放、字幕切换、进度记忆、清晰度切换 | M4 |
| **云下载** | `/offline` | 任务列表、新建链接任务、BT 任务、配额展示 | M5 |
| **回收站** | `/recycle` | 回收站列表、还原、彻底删除、清空 | M6 |
| **分享管理** | `/share` | 我的分享列表、发送/接收记录 | M7 |
| **用户中心** | `/profile` | 个人信息、VIP 状态、余额、空间用量 | M8 |
| **设置** | `/settings` | 用户偏好设置 | M8 |

---

## 四、详细功能需求

### M1 登录认证

**现状**: 115 使用 Cookie（UID/CID/USSID，HttpOnly）做身份认证，无标准 OAuth 流程。

| 用户故事 | API | 优先级 |
|---|---|---|
| 用户打开网站时，检测登录态是否有效 | `GET webapi.115.com/files`（返回 200 即为已登录） | P0 |
| 未登录时，引导用户手动在 115.com 登录后刷新 | — | P0 |
| 显示当前登录用户信息 | `GET my.115.com/proapi/3.0/index.php?method=user_info` | P0 |

### M2 文件管理

#### 文件浏览

| 用户故事 | API | 优先级 |
|---|---|---|
| 查看当前目录下的文件和文件夹列表 | `GET webapi.115.com/files?cid=&limit=&offset=` | P0 |
| 双击文件夹进入子目录 | 同上（更新 cid） | P0 |
| 面包屑导航快速返回上级目录 | 响应中 `path` 数组 | P0 |
| 切换列表/网格视图 | 前端状态 | P0 |
| 按名称/时间/大小排序 | `o=file_name|user_ptime|file_size`, `asc=0|1` | P1 |
| 按文件类型筛选（文档/图片/音乐/视频/压缩包） | `type=1|2|3|4|5` | P1 |
| 分页加载 / 无限滚动 | `offset` + `limit` | P1 |
| 查看文件/文件夹详情（大小、路径、修改时间） | `GET /open/ufile/info?file_id=` | P2 |

#### 文件操作

| 用户故事 | API | 优先级 |
|---|---|---|
| 新建文件夹 | `POST /open/ufile/add` body: `{pid, file_name}` | P0 |
| 重命名文件/文件夹 | `POST /open/ufile/update` body: `{file_id, file_name}` | P0 |
| 删除文件（移入回收站） | `POST /open/ufile/delete` body: `{file_ids, parent_id}` | P0 |
| 移动文件到其他目录 | `POST /open/ufile/move` body: `{file_ids, pid}` | P1 |
| 复制文件到其他目录 | `POST /open/ufile/copy` body: `{file_ids, pid}` | P1 |
| 星标/取消星标 | `POST /open/ufile/update` body: `{file_id, star}` | P2 |
| 获取文件下载地址 | `POST /open/ufile/downurl` body: `{pick_code}` | P0 |

#### 文件搜索

| 用户故事 | API | 优先级 |
|---|---|---|
| 全局搜索文件 | `GET webapi.115.com/files/search?search_value=&limit=&offset=` | P0 |
| 按类型筛选搜索结果 | `type=1\|2\|3\|4\|5` | P1 |
| 按时间范围搜索 | `gte_day=&lte_day=` | P2 |
| 在指定目录下搜索 | `cid=` | P2 |

### M3 文件上传

| 用户故事 | API | 优先级 |
|---|---|---|
| 选择本地文件上传到当前目录 | `POST /open/upload/init`（初始化）→ 秒传或 OSS 上传 | P1 |
| 秒传（SHA1 匹配） | 初始化接口 status=2 | P1 |
| 大文件断点续传 | `POST /open/upload/resume` | P1 |
| 拖拽上传 | 前端实现 | P1 |
| 上传进度条 | 前端实现 | P1 |
| 获取上传凭证 | `GET /open/upload/get_token` | P1 |

### M4 视频播放

| 用户故事 | API | 优先级 |
|---|---|---|
| 在线播放视频文件 | `GET webapi.115.com/files/video?pickcode=` | P1 |
| 切换清晰度（多分辨率） | 响应中 `video_url` 数组 | P1 |
| 加载字幕列表 | `GET webapi.115.com/movies/subtitle?pickcode=` | P1 |
| 切换字幕语言 | 前端状态 | P1 |
| 自动记忆播放进度 | `POST /open/video/history` body: `{pick_code, time}` | P2 |
| 恢复上次播放位置 | `GET /open/video/history?pick_code=` | P2 |
| 提交视频转码 | `POST /open/video/push` body: `{pick_code, op}` | P3 |

### M5 云下载

| 用户故事 | API | 优先级 |
|---|---|---|
| 查看云下载任务列表 | `POST 115.com/web/lixian/?ct=lixian&ac=task_lists` | P1 |
| 查看云下载配额 | `GET 115.com/web/lixian/?ct=lixian&ac=get_quota_package_info` | P1 |
| 查看任务统计（进行中/已完成/失败） | `POST 115.com/web/lixian/?ct=lixian&ac=task_count` | P1 |
| 新建链接下载任务（HTTP/磁力/电驴） | `POST /open/offline/add_task_url` body: `{urls, wp_path_id}` | P1 |
| 新建 BT 下载任务 | `POST /open/offline/add_task_bt` body: `{info_hash, wanted, ...}` | P2 |
| 解析 BT 种子文件 | `POST /open/offline/torrent` body: `{torrent_sha1, pick_code}` | P2 |
| 删除单个任务 | `POST /open/offline/del_task` body: `{info_hash, del_source}` | P1 |
| 清空任务（按类型） | `POST /open/offline/clear_task` body: `{flag}` | P2 |

### M6 回收站

| 用户故事 | API | 优先级 |
|---|---|---|
| 查看回收站文件列表 | `GET webapi.115.com/rb?aid=7&cid=0&offset=&limit=` | P2 |
| 还原文件 | `POST /open/rb/revert` body: `{tid}` | P2 |
| 彻底删除文件 | `POST /open/rb/del` body: `{tid}` | P2 |
| 清空回收站 | `POST /open/rb/del`（不传 tid） | P2 |

### M7 分享管理

| 用户故事 | API | 优先级 |
|---|---|---|
| 查看我的分享列表 | `GET webapi.115.com/share/slist?offset=&limit=` | P2 |
| 查看发送分享记录 | `GET webapi.115.com/share/send?offset=&limit=` | P2 |
| 查看接收分享记录 | `GET webapi.115.com/share/receive?offset=&limit=` | P2 |

### M8 用户中心

| 用户故事 | API | 优先级 |
|---|---|---|
| 查看个人信息 | `GET my.115.com/proapi/3.0/index.php?method=user_info` | P2 |
| 查看 VIP 状态 | `GET webapi.115.com/user/vip_limit?feature=2` | P2 |
| 查看账户余额 | `GET pay.115.com/?ct=balance&ac=getBalance` | P3 |
| 查看存储空间用量 | `GET webapi.115.com/files/index_info?count_space_nums=1` | P1 |
| 查看标签列表 | `GET webapi.115.com/label/list?user_id=&offset=&limit=` | P2 |

---

## 五、API 清单汇总

### 5.1 来源: 网页端流量采集（已验证可用）

| # | 方法 | 域名 | 路径 | 用途 | 模块 |
|---|---|---|---|---|---|
| 1 | GET | webapi.115.com | `/files` | 文件列表 | M2 |
| 2 | GET | webapi.115.com | `/files/search` | 文件搜索 | M2 |
| 3 | GET | webapi.115.com | `/files/video` | 视频播放地址 | M4 |
| 4 | GET | webapi.115.com | `/files/index_info` | 空间信息 | M8 |
| 5 | GET | webapi.115.com | `/movies/subtitle` | 视频字幕 | M4 |
| 6 | GET | webapi.115.com | `/rb` | 回收站列表 | M6 |
| 7 | GET | webapi.115.com | `/share/slist` | 我的分享 | M7 |
| 8 | GET | webapi.115.com | `/share/send` | 发送分享 | M7 |
| 9 | GET | webapi.115.com | `/share/receive` | 接收分享 | M7 |
| 10 | GET | webapi.115.com | `/label/list` | 标签列表 | M8 |
| 11 | GET | webapi.115.com | `/category/shortcut` | 分类快捷方式 | M2 |
| 12 | GET | webapi.115.com | `/user/setting` | 用户设置 | M8 |
| 13 | GET | webapi.115.com | `/user/vip_limit` | VIP 限制 | M8 |
| 14 | GET | webapi.115.com | `/user/allow_protocol` | 协议权限 | M8 |
| 15 | GET | webapi.115.com | `/user/fingerprint` | 用户指纹 | M1 |
| 16 | GET | webapi.115.com | `/usershare/list` | 用户分享列表 | M7 |
| 17 | POST | 115.com | `/web/lixian/?ac=task_lists` | 云下载任务列表 | M5 |
| 18 | GET | 115.com | `/web/lixian/?ac=get_quota_package_info` | 云下载配额 | M5 |
| 19 | POST | 115.com | `/web/lixian/?ac=task_count` | 任务统计 | M5 |
| 20 | GET | my.115.com | `/proapi/3.0/index.php?method=user_info` | 用户信息 | M8 |
| 21 | GET | vip.115.com | `/?ct=order&ac=get_yun_card` | VIP 卡信息 | M8 |
| 22 | GET | pay.115.com | `/?ct=balance&ac=getBalance` | 余额 | M8 |
| 23 | GET | act.115.com | `/api/1.0/web/1.0/index/act_switch` | 活动开关 | M9 |
| 24 | GET | msg.115.com | `/?ct=contacts&ac=notice` | 消息通知 | M9 |
| 25 | GET | home.115.com | `/api/1.0/web/1.0/user/unread` | 未读消息 | M9 |

### 5.2 来源: 开放平台文档（待集成）

| # | 方法 | 路径 | 用途 | 模块 |
|---|---|---|---|---|
| 26 | GET | `/open/ufile/files` | 文件列表（开放平台版） | M2 |
| 27 | GET | `/open/ufile/search` | 文件搜索（开放平台版） | M2 |
| 28 | POST | `/open/ufile/add` | 新建文件夹 | M2 |
| 29 | POST | `/open/ufile/update` | 重命名/星标 | M2 |
| 30 | POST | `/open/ufile/delete` | 删除文件 | M2 |
| 31 | POST | `/open/ufile/move` | 移动文件 | M2 |
| 32 | POST | `/open/ufile/copy` | 复制文件 | M2 |
| 33 | POST | `/open/ufile/downurl` | 获取下载地址 | M2 |
| 34 | GET | `/open/ufile/info` | 文件详情 | M2 |
| 35 | POST | `/open/upload/init` | 上传初始化 | M3 |
| 36 | GET | `/open/upload/get_token` | 上传凭证 | M3 |
| 37 | POST | `/open/upload/resume` | 断点续传 | M3 |
| 38 | GET | `/open/video/play` | 视频播放地址 | M4 |
| 39 | GET | `/open/video/subtitle` | 字幕列表 | M4 |
| 40 | GET | `/open/video/history` | 获取播放进度 | M4 |
| 41 | POST | `/open/video/history` | 保存播放进度 | M4 |
| 42 | POST | `/open/video/push` | 视频转码 | M4 |
| 43 | GET | `/open/offline/get_task_list` | 云下载任务列表 | M5 |
| 44 | GET | `/open/offline/get_quota_info` | 云下载配额 | M5 |
| 45 | POST | `/open/offline/add_task_url` | 添加链接任务 | M5 |
| 46 | POST | `/open/offline/add_task_bt` | 添加 BT 任务 | M5 |
| 47 | POST | `/open/offline/torrent` | 解析 BT 种子 | M5 |
| 48 | POST | `/open/offline/del_task` | 删除任务 | M5 |
| 49 | POST | `/open/offline/clear_task` | 清空任务 | M5 |
| 50 | GET | `/open/rb/list` | 回收站列表 | M6 |
| 51 | POST | `/open/rb/revert` | 回收站还原 | M6 |
| 52 | POST | `/open/rb/del` | 删除/清空回收站 | M6 |
| 53 | GET | `/open/user/info` | 用户信息 | M8 |

---

## 六、技术约束

| 约束项 | 说明 |
|---|---|
| **CORS** | 115 API 不允许跨域直接调用，需通过 Vite dev proxy 或 BFF 转发 |
| **鉴权** | Cookie-based（UID/CID/USSID 均为 HttpOnly），需代理层转发 Cookie |
| **域名分散** | 涉及 15 个子域名，代理配置需覆盖全部 |
| **SPA 路由** | 115 原站是 query-string 路由（`?cid=&tab=`），新站用 React Router path 路由 |
| **响应格式** | 部分 API 返回 `state: boolean` + `data`，部分返回 `state: number`，需统一适配 |
| **写操作** | 文件删除/移动/复制等为破坏性操作，需二次确认 |

---

## 七、开发阶段规划

| 阶段 | 范围 | 交付物 |
|---|---|---|
| **Phase 1** | M1 + M2（文件浏览/搜索/基本操作）+ 布局框架 | 可用的文件管理器 |
| **Phase 2** | M4（视频播放）+ M5（云下载） | 多媒体 + 下载管理 |
| **Phase 3** | M3（上传）+ M6（回收站）+ M7（分享） | 完整文件生命周期 |
| **Phase 4** | M8（用户中心）+ M9（消息）+ 细节优化 | 完善体验 |

---

## 八、技术选型（待确认）

| 类别 | 选型 | 说明 |
|---|---|---|
| 框架 | React 19 + Vite | 快速开发 + HMR |
| UI 库 | shadcn/ui + TailwindCSS 4 | 现代化组件 + 样式 |
| 图标 | Lucide React | 统一图标库 |
| 状态管理 | Zustand | 轻量状态管理 |
| 路由 | React Router v7 | SPA 路由 |
| 请求 | TanStack Query + fetch | 缓存 + 请求管理 |
| 视频播放 | HLS.js + 自定义播放器 | HLS 流播放 |
| 虚拟列表 | TanStack Virtual | 大文件列表性能 |
| 拖拽 | dnd-kit | 文件拖拽操作 |
