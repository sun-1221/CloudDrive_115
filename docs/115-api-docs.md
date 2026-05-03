# 115 网页端接口文档

> 基于自动化流量采集生成  
> runId: `20260408145821-oqg9xv`  
> 自动采集时间: 2026-04-08  
> 手动补充采集: 2026-04-18（上传 `uplb.115.com` + OSS 直传、转码提交、播放进度读写）

## 概览

| 指标 | 数值 |
|---|---|
| 唯一接口数 | 71（65 自动 + 6 手动补充） |
| GET 接口 | 64 |
| POST 接口 | 7 |
| 涉及域名 | 17 |
| 采集场景 | files, home, offline, recycle, search, share, video, upload¹, transcode¹, video-history¹ |

> ¹ 手动补充采集场景（2026-04-18），详见 `collector/output/manual-capture/manual-20260418132315-summary.md`

---

## webapi.115.com

> 业务接口: 23 个（19 自动 + 4 手动补充）

### `GET` /category/shortcut

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/category/shortcut` |
| **样本数** | 10 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errno": "string",
  "data": "object"
}
```

### `GET` /files

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files?aid=1&cid=0&o=user_ptime&asc=0&offset=0&show_dir=1&limit=40&code=&scid=&snap=0&natsort=1&record_open_time=1&count_folders` |
| **样本数** | 10 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `aid` | number |
| `cid` | number |
| `o` | string |
| `asc` | number |
| `offset` | number |
| `show_dir` | number |
| `limit` | number |
| `code` | empty |
| `scid` | empty |
| `snap` | number |
| `natsort` | number |
| `record_open_time` | number |
| `count_folders` | number |
| `type` | empty |
| `source` | empty |
| `format` | string |

**响应体 Schema:**

```json
{
  "data": "object",
  "count": "number",
  "sys_count": "number",
  "file_count": "number",
  "folder_count": "number",
  "page_size": "number",
  "aid": "string",
  "cid": "number",
  "is_asc": "number",
  "star": "number",
  "is_share": "number",
  "type": "number",
  "is_q": "number",
  "r_all": "number",
  "stdir": "number",
  "cur": "number",
  "min_size": "number",
  "max_size": "number",
  "record_open_time": "string",
  "path": "object",
  "fields": "string",
  "order": "string",
  "fc_mix": "number",
  "cost_time_1": "number",
  "natsort": "number",
  "uid": "number",
  "offset": "number",
  "limit": "number",
  "suffix": "string",
  "last_utime": "number",
  "use_cache": "number",
  "cost_time_2": "number",
  "cost_time_3": "number",
  "cost_time_4": "number",
  "cost_time_5": "number",
  "cost_time_6": "number",
  "cost_time_7": "number",
  "cost_time_8": "number",
  "state": "boolean",
  "error": "string",
  "errNo": "number"
}
```

### `GET` /files

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files?aid=1&cid=0&o=user_ptime&asc=0&offset=0&show_dir=1&limit=30&code=&scid=&snap=0&natsort=1&star=1&source=&format=json` |
| **样本数** | 13 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `aid` | number |
| `cid` | number |
| `o` | string |
| `asc` | number |
| `offset` | number |
| `show_dir` | number |
| `limit` | number |
| `code` | empty |
| `scid` | empty |
| `snap` | number |
| `natsort` | number |
| `star` | number |
| `source` | empty |
| `format` | string |

**响应体 Schema:**

```json
{
  "data": "object",
  "count": "number",
  "sys_count": "number",
  "page_size": "number",
  "aid": "string",
  "cid": "number",
  "is_asc": "number",
  "star": "number",
  "is_share": "number",
  "type": "number",
  "is_q": "number",
  "r_all": "number",
  "stdir": "number",
  "cur": "number",
  "min_size": "number",
  "max_size": "number",
  "record_open_time": "string",
  "path": "object",
  "fields": "string",
  "order": "string",
  "fc_mix": "number",
  "cost_time_1": "number",
  "natsort": "number",
  "uid": "number",
  "offset": "number",
  "limit": "number",
  "suffix": "string",
  "last_utime": "number",
  "use_cache": "number",
  "cost_time_2": "number",
  "cost_time_3": "number",
  "cost_time_4": "number",
  "cost_time_5": "number",
  "cost_time_6": "number",
  "cost_time_7": "number",
  "cost_time_8": "number",
  "state": "boolean",
  "error": "string",
  "errNo": "number"
}
```

### `GET` /files

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files?aid=1&cid=0&o=user_ptime&asc=0&offset=0&show_dir=0&limit=50&type=4&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `aid` | number |
| `cid` | number |
| `o` | string |
| `asc` | number |
| `offset` | number |
| `show_dir` | number |
| `limit` | number |
| `type` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "data": {
    "_type": "array",
    "_itemShape": {
      "fid": "string",
      "uid": "number",
      "aid": "number",
      "cid": "string",
      "n": "string",
      "s": "number",
      "sta": "number",
      "pt": "string",
      "pc": "string",
      "fc": "number",
      "p": "number",
      "m": "number",
      "star_time": "number",
      "t": "string",
      "te": "string",
      "tp": "string",
      "tu": "string",
      "to": "number",
      "d": "number",
      "c": "number",
      "sh": "number",
      "e": "string",
      "ico": "string",
      "class": "string",
      "fatr": "string",
      "fdes": "number",
      "sha": "string",
      "q": "number",
      "hdf": "number",
      "et": "number",
      "epos": "string",
      "fvs": "number",
      "check_code": "number",
      "check_msg": "string",
      "fuuid": "number",
      "ec": "number",
      "fl": {
        "_type": "array",
        "_itemShape": null,
        "_length": 0
      },
      "u": "string",
      "iv": "number",
      "score": "number",
      "is_top": "number",
      "play_long": "number"
    },
    "_length": 1
  },
  "count": "number",
  "sys_count": "number",
  "page_size": "number",
  "aid": "string",
  "cid": "number",
  "is_asc": "number",
  "star": "number",
  "is_share": "number",
  "type": "number",
  "is_q": "number",
  "r_all": "number",
  "stdir": "number",
  "cur": "number",
  "min_size": "number",
  "max_size": "number",
  "record_open_time": "string",
  "path": {
    "_type": "array",
    "_itemShape": {
      "name": "string",
      "aid": "string",
      "cid": "string",
      "pid": "string",
      "isp": "string"
    },
    "_length": 1
  },
  "fields": "string",
  "order": "string",
  "fc_mix": "number",
  "cost_time_1": "number",
  "natsort": "number",
  "uid": "number",
  "offset": "number",
  "limit": "number",
  "suffix": "string",
  "last_utime": "number",
  "use_cache": "number",
  "cost_time_2": "number",
  "cost_time_3": "number",
  "cost_time_4": "number",
  "cost_time_5": "number",
  "cost_time_6": "number",
  "cost_time_7": "number",
  "cost_time_8": "number",
  "state": "boolean",
  "error": "string",
  "errNo": "number"
}
```

### `GET` /files/index_info

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/index_info` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "data": "object",
  "error": "string",
  "code": "number"
}
```

### `GET` /files/index_info

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/index_info?count_space_nums=1` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `count_space_nums` | number |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "data": "object",
  "error": "string",
  "code": "number"
}
```

### `GET` /files/search

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/search?offset=0&limit=30&search_value=%E8%A7%86%E9%A2%91&date=&aid=1&cid=0&pick_code=&type=&count_folders=1&source=&forma` |
| **样本数** | 3 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | search |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `offset` | number |
| `limit` | number |
| `search_value` | string |
| `date` | empty |
| `aid` | number |
| `cid` | number |
| `pick_code` | empty |
| `type` | empty |
| `count_folders` | number |
| `source` | empty |
| `format` | string |

**响应体 Schema:**

```json
{
  "count": "number",
  "data": "object",
  "cost_time_es": "number",
  "folder": "object",
  "page_size": "number",
  "page_count": "number",
  "offset": "number",
  "is_asc": "number",
  "order": "string",
  "suffix": "string",
  "fc_mix": "number",
  "type": "number",
  "state": "boolean",
  "error": "string",
  "errCode": "number"
}
```

### `GET` /files/video

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/video?pickcode=ekmkz2js4zqfpzfrh&share_id=0&local=1&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `pickcode` | string |
| `share_id` | number |
| `local` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "errno": "number",
  "error": "string"
}
```

### `GET` /label/list

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/label/list?user_id=103019297&offset=0&limit=11500` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `user_id` | number |
| `offset` | number |
| `limit` | number |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

### `GET` /movies/subtitle

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/movies/subtitle?pickcode=ekmkz2js4zqfpzfrh&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `pickcode` | string |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errNo": "number",
  "data": {
    "autoload": {
      "_type": "array",
      "_itemShape": null,
      "_length": 0
    },
    "list": {
      "_type": "array",
      "_itemShape": null,
      "_length": 0
    }
  }
}
```

### `GET` /rb

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/rb?aid=7&cid=0&offset=0&limit=30&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | recycle |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `aid` | number |
| `cid` | number |
| `offset` | number |
| `limit` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "count": "string",
  "rb_pass": "number",
  "state": "boolean",
  "error": "string",
  "offset": "number",
  "page_size": "number",
  "order": "string",
  "is_asc": "number"
}
```

### `GET` /share/receive

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/share/receive?offset=0&limit=30&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | share |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `offset` | number |
| `limit` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errNo": "number",
  "errno": "number",
  "request": "string"
}
```

### `GET` /share/send

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/share/send?offset=0&limit=30&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | share |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `offset` | number |
| `limit` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errNo": "number",
  "errno": "number",
  "request": "string"
}
```

### `GET` /share/slist

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/share/slist?offset=0&limit=30&format=json` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | share |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `offset` | number |
| `limit` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errno": "number",
  "count": "number",
  "list": {
    "_type": "array",
    "_itemShape": null,
    "_length": 0
  },
  "define_receive_code": "number",
  "bind_receive_code": "number"
}
```

### `GET` /user/allow_protocol

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/user/allow_protocol?action=global` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `action` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errno": "number",
  "data": "object"
}
```

### `GET` /user/fingerprint

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/user/fingerprint` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "message": "string",
  "code": "string",
  "data": "object"
}
```

### `GET` /user/setting

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/user/setting` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errno": "number",
  "data": "object"
}
```

### `GET` /user/vip_limit

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/user/vip_limit?feature=2` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `feature` | number |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "message": "string",
  "code": "string",
  "data": "object"
}
```

### `GET` /usershare/list

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/usershare/list` |
| **样本数** | 13 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "errno": "string",
  "data": "object"
}
```

### `GET` /files/video_push

> ⚠️ 手动补充（2026-04-18 transcode 场景）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/video_push?pickcode={pickcode}&op=vip_push&format=json` |
| **样本数** | 1 |
| **置信度** | low（场景脚本参考） |
| **状态码** | 200 |
| **鉴权** | cookie |
| **触发场景** | transcode |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `pickcode` | string |
| `op` | string |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "errNo": "number",
  "error": "string",
  "errno": "number",
  "data": "string"
}
```

### `GET` /movies/transcode

> ⚠️ 手动补充（2026-04-18 transcode 场景，备选路径）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/movies/transcode?pickcode={pickcode}&format=json` |
| **样本数** | 1 |
| **置信度** | low（场景脚本参考） |
| **状态码** | 200 |
| **鉴权** | cookie |
| **触发场景** | transcode |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `pickcode` | string |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "errNo": "number",
  "error": "string",
  "errno": "number"
}
```

### `GET` /files/history

> ⚠️ 手动补充（2026-04-18 video-history 场景）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/history?pick_code={pickcode}&fetch=one&category=1&format=json` |
| **样本数** | 1 |
| **置信度** | low（场景脚本参考） |
| **状态码** | 200 |
| **鉴权** | cookie |
| **触发场景** | video-history |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `pick_code` | string |
| `fetch` | string |
| `category` | number |
| `format` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string",
  "data": "object"
}
```

> 说明：`data` 包含 `file_id`、`file_name`、`hash`、`pick_code`、`time`（已播放秒数）、`add_time`（记录时间戳）

### `POST` /files/history

> ⚠️ 手动补充（2026-04-18 video-history 场景）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://webapi.115.com/files/history` |
| **样本数** | 1 |
| **置信度** | low（场景脚本参考） |
| **状态码** | 200 |
| **鉴权** | cookie |
| **触发场景** | video-history |

**请求体（application/x-www-form-urlencoded）:**

| 参数名 | 类型 |
|---|---|
| `pick_code` | string |
| `time` | number |
| `category` | number |
| `op` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "error": "string"
}
```

### 静态资源 (1 个)

| 方法 | 路径 | 样本数 | 场景 |
|---|---|---|---|
| GET | `/bridge_2.0.html` | 13 | files, offline, recycle, search, share, video |

---

## 115.com

> 业务接口: 14 个

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?cid=0&offset=0&tab=&mode=wangpan` |
| **样本数** | 7 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `cid` | number |
| `offset` | number |
| `tab` | empty |
| `mode` | string |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?ct=file&ac=userfile&tpl=view_large&s=0&is_wl_tpl=1&aid=1&cid=0&offset=0&limit=24&tab=` |
| **样本数** | 7 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `tpl` | string |
| `s` | number |
| `is_wl_tpl` | number |
| `aid` | number |
| `cid` | number |
| `offset` | number |
| `limit` | number |
| `tab` | empty |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | home |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?tab=offline&mode=wangpan` |
| **样本数** | 3 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | offline, recycle, share |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `tab` | string |
| `mode` | string |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?ct=index&ac=offline_new_tpl&offline=1&tab=offline` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | offline |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `offline` | number |
| `tab` | string |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?ct=file&ac=userfile&tpl=view_large&s=0&is_wl_tpl=1&aid=1&cid=-1&offset=0&limit=24&tab=recycle` |
| **样本数** | 2 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | recycle, share |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `tpl` | string |
| `s` | number |
| `is_wl_tpl` | number |
| `aid` | number |
| `cid` | string |
| `offset` | number |
| `limit` | number |
| `tab` | string |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?mode=search&submode=wangpan&url=%2F%2F115.com%2F%3Faid%3D1%26cid%3D0%26old_cid%3D0%26old_cid_name%3D%25E6%25A0%25B9%25E7%259B%25AE%25` |
| **样本数** | 3 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | search |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `mode` | string |
| `submode` | string |
| `url` | string |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?aid=1&cid=0&old_cid=0&old_cid_name=%E6%A0%B9%E7%9B%AE%E5%BD%95&search_value=%E8%A7%86%E9%A2%91&ct=file&ac=search&is_wl_tpl=1` |
| **样本数** | 3 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | search |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `aid` | number |
| `cid` | number |
| `old_cid` | number |
| `old_cid_name` | string |
| `search_value` | string |
| `ct` | string |
| `ac` | string |
| `is_wl_tpl` | number |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?cid=0&offset=0&mode=wangpan&type=4` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `cid` | number |
| `offset` | number |
| `mode` | string |
| `type` | number |

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?ct=file&ac=userfile&tpl=view_large&s=0&is_wl_tpl=1&aid=1&cid=0&offset=0&limit=24&type=4` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `tpl` | string |
| `s` | number |
| `is_wl_tpl` | number |
| `aid` | number |
| `cid` | number |
| `offset` | number |
| `limit` | number |
| `type` | number |

### `POST` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/?ac=setting&even=saveedit&is_wl_tpl=1` |
| **样本数** | 10 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ac` | string |
| `even` | string |
| `is_wl_tpl` | number |

**请求体:**

```json
{
  "last_file_type": "string",
  "last_file_id": "number"
}
```

**响应体 Schema:**

```json
{
  "state": "boolean",
  "goto": "string",
  "even": "string",
  "data": "object",
  "flush": "boolean"
}
```

### `GET` /web/lixian/

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/web/lixian/?ct=lixian&ac=get_quota_package_info` |
| **样本数** | 3 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | offline |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |

**响应体 Schema:**

```json
{
  "package": "object",
  "count": "number",
  "surplus": "number",
  "max_size": "number",
  "used": "number"
}
```

### `POST` /web/lixian/

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/web/lixian/?ct=lixian&ac=task_lists` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | offline |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |

**请求体:**

```json
{
  "page": "number",
  "stat": "number",
  "uid": "number",
  "sign": "string",
  "time": "number"
}
```

**响应体 Schema:**

```json
{
  "page": "number",
  "page_count": "number",
  "page_row": "number",
  "page_size": "number",
  "count": "number",
  "quota": "number",
  "total": "number",
  "tasks": "object",
  "state": "boolean",
  "real_state": "boolean",
  "errtype": "string"
}
```

### `POST` /web/lixian/

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://115.com/web/lixian/?ct=lixian&ac=task_count` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | offline |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |

**响应体 Schema:**

```json
{
  "total_count": "number",
  "failed_count": "number",
  "finished_count": "number",
  "downloading_count": "number",
  "state": "boolean",
  "errtype": "string"
}
```

### 静态资源 (1 个)

| 方法 | 路径 | 样本数 | 场景 |
|---|---|---|---|
| GET | `/static/cross.html` | 14 | files, offline, recycle, search, share, video |

---

## my.115.com

> 业务接口: 1 个

### `GET` /proapi/3.0/index.php

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://my.115.com/proapi/3.0/index.php?method=user_info&uid=103019297` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | home |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `method` | string |
| `uid` | number |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "code": "number",
  "message": "string",
  "data": {
    "user_face": "string",
    "user_face_ori": "string",
    "violation_info": {
      "_type": "array",
      "_itemShape": null,
      "_length": 0
    },
    "checking_info": {
      "_type": "array",
      "_itemShape": null,
      "_length": 0
    },
    "user_id": "string",
    "user_name": "string",
    "display_uid": "string",
    "is_vip": "number",
    "mark": "string",
    "mark2": "number",
    "mark3": "number",
    "birthday": "string",
    "birthday_show": "string",
    "star": "string",
    "calendar_type": "string",
    "user_name_checking": "boolean",
    "user_name_prepub": "string",
    "gender": "string",
    "gender_show": "string",
    "blood_type": "string",
    "blood_type_show": "string",
    "is_marry": "string",
    "is_marry_show": "string",
    "location": "string",
    "location_show": "string",
    "location_birth": "string",
    "location_birth_show": "string",
    "location_link": "string",
    "location_link_show": "string",
    "address": "string",
    "pub_mobile": "string",
    "pub_mobile_mask": "string",
    "pub_email": "string",
    "pub_email_mask": "string",
    "homepage": "string",
    "salary": "string",
    "height": "number",
    "weight": "number",
    "education": "number",
    "like_celeb": "string",
    "wechat": "string",
    "weibo": "string",
    "alipay": "string",
    "like_music": "string",
    "like_animal": "string",
    "like_book": "string",
    "like_video": "string",
    "qrcode_url": "string",
    "share_url": "string",
    "interest": {
      "_type": "array",
      "_itemShape": null,
      "_length": 0
    },
    "job": "number",
    "job_show": "string",
    "user_public": {
      "profile_only_me": "number",
      "gender": "number",
      "birthday": "number",
      "height": "number",
      "weight": "number",
      "blood_type": "number",
      "is_marry": "number",
      "sign": "number",
      "education": "number",
      "job": "number",
      "salary": "number",
      "location_birth": "number",
      "location": "number",
      "location_link": "number",
      "address": "number",
      "wechat": "number",
      "weibo": "number",
      "pub_mobile": "number",
      "pub_email": "number",
      "homepage": "number",
      "alipay": "number",
      "like": "number",
      "interest": "number",
      "home_only_me": "number",
      "home_following_count": "number",
      "home_follower_count": "number",
      "home_likes_count": "number",
      "home_star_list": "number",
      "home_history_list": "number",
      "home_create_list": "number",
      "home_replies_list": "number",
      "q_only_me": "number",
      "q_created_count": "number",
      "q_joined_count": "number",
      "q_fav_list": "number",
      "q_my_q_list": "number",
      "q_history_list": "number",
      "q_create_list": "number",
      "q_replies_list": "number",
      "topic_count": "number",
      "reply_count": "number",
      "qtopic_count": "number",
      "follow_group": "number"
    }
  }
}
```

---

## vip.115.com

> 业务接口: 1 个

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://vip.115.com/?ct=order&ac=get_yun_card` |
| **样本数** | 29 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |

**响应体 Schema:**

```json
{
  "data": {
    "num": "number",
    "url": "string",
    "vip_count": "number",
    "space_count": "number",
    "forever_count": "number",
    "sqcode_count": "string",
    "sqcode_cancel_count": "string",
    "00_balance": "number"
  },
  "state": "boolean"
}
```

---

## w.115.com

### 静态资源 (3 个)

| 方法 | 路径 | 样本数 | 场景 |
|---|---|---|---|
| GET | `/m_r/web/static/vue/app_down.vue` | 1 | home |
| GET | `/m_r/web/static/vue/login.vue` | 1 | home |
| GET | `/m_r/web/static/vue/share.vue` | 1 | home |

---

## act.115.com

> 业务接口: 1 个

### `GET` /api/1.0/web/1.0/index/act_switch

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://act.115.com/api/1.0/web/1.0/index/act_switch` |
| **样本数** | 15 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, home, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "number",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

---

## appversion.115.com

> 业务接口: 1 个

### `GET` /1/web/1.0/api/getMultiVer

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://appversion.115.com/1/web/1.0/api/getMultiVer` |
| **样本数** | 1 |
| **置信度** | low |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | home |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "code": "number",
  "message": "string",
  "data": {
    "Android": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "iOS-iPhone": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "PC-115chrome": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "Android-tv": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "Android-Yun": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "iOS-iPhone-Yun": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "MAC-115chrome": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "window_115": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "mac_115": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "linux_115": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "window_115_x64": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "PC-115chrome_x64": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "MAC-Arm-115chrome": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115wangpan_android": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115wangpan_iOS": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115wangpan_hongmeng": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "Linux-115chrome": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115xiaochengxu_weixin": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115Life_Windows_X32": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115Life_Windows_X64": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115Life_macOS_Intel": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "115Life_macOS_Arm": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    },
    "apple-tv": {
      "app_os": "number",
      "version_code": "string",
      "created_time": "number",
      "version_url": "string"
    }
  }
}
```

---

## pay.115.com

> 业务接口: 1 个

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://pay.115.com/?ct=balance&ac=getBalance&from=vip&_=1775660331753` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `from` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "code": "number",
  "msg": "string",
  "data": "object"
}
```

---

## msg.115.com

> 业务接口: 1 个

### `GET` /

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://msg.115.com/?ct=contacts&ac=notice&client=web` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `ct` | string |
| `ac` | string |
| `client` | string |

**响应体 Schema:**

```json
{
  "state": "number",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

---

## yunmsg.115.com

> 业务接口: 1 个

### `GET` /api/1.0/web/1.0/1/job_contacts/applicant

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://yunmsg.115.com/api/1.0/web/1.0/1/job_contacts/applicant` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "number",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

---

## pmsg.115.com

> 业务接口: 1 个

### `GET` /api/1.0/web/1.0/contact/ls

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://pmsg.115.com/api/1.0/web/1.0/contact/ls` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

---

## home.115.com

> 业务接口: 1 个

### `GET` /api/1.0/web/1.0/user/unread

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://home.115.com/api/1.0/web/1.0/user/unread` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "number",
  "code": "number",
  "message": "string",
  "data": "object"
}
```

---

## q.115.com

> 业务接口: 2 个

### `POST` /api/q_api_home.php

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://q.115.com/api/q_api_home.php` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**请求体:**

```json
{
  "api_method": "string"
}
```

**响应体 Schema:**

```json
{
  "data": "object",
  "errCode": "number",
  "message": "string",
  "state": "boolean"
}
```

### `GET` /mapp/

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://q.115.com/mapp/?c=user&m=check_yun_once_joined` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**Query 参数:**

| 参数名 | 类型 |
|---|---|
| `c` | string |
| `m` | string |

**响应体 Schema:**

```json
{
  "state": "boolean",
  "message": "string",
  "code": "number",
  "data": "boolean"
}
```

### 静态资源 (14 个)

| 方法 | 路径 | 样本数 | 场景 |
|---|---|---|---|
| GET | `/bridge_assets.html` | 1 | files |
| GET | `/bridge_assets.html` | 1 | offline |
| GET | `/bridge_assets.html` | 1 | offline |
| GET | `/bridge_assets.html` | 1 | recycle |
| GET | `/bridge_assets.html` | 1 | recycle |
| GET | `/bridge_assets.html` | 1 | search |
| GET | `/bridge_assets.html` | 1 | search |
| GET | `/bridge_assets.html` | 1 | search |
| GET | `/bridge_assets.html` | 1 | search |
| GET | `/bridge_assets.html` | 1 | search |
| GET | `/bridge_assets.html` | 1 | share |
| GET | `/bridge_assets.html` | 1 | share |
| GET | `/bridge_assets.html` | 1 | video |
| GET | `/bridge_assets.html` | 1 | video |

---

## passportapi.115.com

> 业务接口: 1 个

### `GET` /app/1.0/web/1.0/user/warning_get

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://passportapi.115.com/app/1.0/web/1.0/user/warning_get` |
| **样本数** | 14 |
| **置信度** | medium |
| **状态码** | 200 |
| **鉴权** | none |
| **触发场景** | files, offline, recycle, search, share, video |

**响应体 Schema:**

```json
{
  "state": "number",
  "code": "number",
  "errno": "number",
  "data": "object",
  "message": "string",
  "error": "string"
}
```

---

## cdnassets.115.com

### 静态资源 (1 个)

| 方法 | 路径 | 样本数 | 场景 |
|---|---|---|---|
| GET | `/bridge.html` | 14 | files, offline, recycle, search, share, video |

---

## uplb.115.com

> 业务接口: 1 个（手动补充 2026-04-18）

### `POST` /3.0/sampleinitupload.php

> ⚠️ 手动补充（2026-04-18 upload 场景）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://uplb.115.com/3.0/sampleinitupload.php` |
| **样本数** | 2 |
| **置信度** | high（手动抓包确认） |
| **状态码** | 200 |
| **鉴权** | cookie + userid 参数 |
| **触发场景** | upload |

**请求体（application/x-www-form-urlencoded）:**

| 参数名 | 类型 | 说明 |
|---|---|---|
| `userid` | number | 115 用户 ID |
| `filename` | string | 文件名 |
| `filesize` | number | 文件大小（字节） |
| `target` | string | 目标目录，格式 `U_1_{cid}` |

**响应体 Schema（非秒传 — 返回 OSS 直传凭证）:**

```json
{
  "object": "string",
  "accessid": "string",
  "host": "string",
  "policy": "string (base64)",
  "signature": "string",
  "expire": "number",
  "callback": "string (base64)"
}
```

> 说明：`Content-Type` 为 `text/html`，实际包裹 JSON。秒传时响应含 `file_id` 字段且无 OSS 凭证。`callback` base64 解码后包含 `callbackUrl: http://uplb.115.com/3.0/samplecompleteupload.php` 和 `callbackBody` 模板。

**响应样本（非秒传）:**

```json
{"object":"69e385e5f0cf326353f8219b058e5ca61bd86969","accessid":"LTAI5tMvF188zm8v4bVG2BQC","host":"https://fhnfile.oss-cn-shenzhen.aliyuncs.com","policy":"eyJleHBpcmF0aW9uIjoiMjAyNi0wNC0xOFQyMToyNDoxOVoiLCJjb25kaXRpb25zIjpbWyJjb250ZW50LWxlbmd0aC1yYW5nZSIsMCw1MzY4NzA5MTIwXV19","signature":"c2ou6/wIDnfGyRS7ieQP4rgJBfo=","expire":1776518659,"callback":"eyJjYWxsYmFja1VybCI6Imh0dHA6XC9cL3VwbGIuMTE1LmNvbVwvMy4wXC9zYW1wbGVjb21wbGV0ZXVwbG9hZC5waHAiLCJjYWxsYmFja0JvZHkiOiJzaGExPSR7c2hhMX0mZmlsZW5hbWU9JHtvYmplY3R9JmZpbGVzaXplPSR7c2l6ZX0mbWltZVR5cGU9JHttaW1lVHlwZX0maGVpZ2h0PSR7aW1hZ2VJbmZvLmhlaWdodH0md2lkdGg9JHtpbWFnZUluZm8ud2lkdGh9JnVzZXJpZD0xMDMwMTkyOTcmdGFyZ2V0PVVfMV8wJnVzZXJmbj1lR2x1WjJoMWJ5MTNhVzVrYzNWeVppMVRaWFIxY0MwNExqTXVOeTE0TmpRdVpYaGwmYnVja2V0PWZobmZpbGUmdXNlcmlwPTExMy44OC4yMjAuMjYmdXNlcnBvcnQ9MjkwNzYmc291cmNlPTQmY2JfdG9rZW49MTIxYTJiYjRmYTE1NDkxMmQxY2E4ZDA0MjA5MGIyZWYiLCJjYWxsYmFja0JvZHlUeXBlIjoiYXBwbGljYXRpb25cL3gtd3d3LWZvcm0tdXJsZW5jb2RlZCJ9"}
```

---

## fhnfile.oss-cn-shenzhen.aliyuncs.com

> 业务接口: 1 个（手动补充 2026-04-18）  
> 说明：阿里云 OSS 对象存储直传，域名为 `{bucket}.oss-cn-{region}.aliyuncs.com`，bucket 名称可能变化

### `POST` /

> ⚠️ 手动补充（2026-04-18 upload 场景 — OSS PostObject）

| 属性 | 值 |
|---|---|
| **完整 URL** | `https://fhnfile.oss-cn-shenzhen.aliyuncs.com/` |
| **样本数** | 1 |
| **置信度** | high（手动抓包确认） |
| **状态码** | 200 |
| **鉴权** | OSS policy + signature（无 Cookie） |
| **触发场景** | upload |

**请求体（multipart/form-data）:**

| 参数名 | 类型 | 说明 |
|---|---|---|
| `key` | string | 即 sampleinitupload 返回的 `object` |
| `policy` | string | base64 编码的 OSS policy |
| `OSSAccessKeyId` | string | 即 sampleinitupload 返回的 `accessid` |
| `signature` | string | OSS 签名 |
| `callback` | string | base64 编码的 callback 配置 |
| `file` | binary | 文件内容 |

**响应体 Schema（OSS callback 透传 115 业务结果）:**

```json
{
  "state": "boolean",
  "message": "string",
  "code": "number",
  "data": {
    "aid": "number",
    "area_id": "number",
    "cid": "string",
    "file_name": "string",
    "file_ptime": "number",
    "file_status": "number",
    "file_id": "string",
    "file_size": "string",
    "pick_code": "string",
    "sha1": "string",
    "sp": "number",
    "file_type": "number",
    "user_id": "string",
    "is_video": "number"
  }
}
```

> 说明：响应实际由 OSS 服务端回调 `uplb.115.com/3.0/samplecompleteupload.php` 后透传给客户端。

**响应样本:**

```json
{"state":true,"message":"","code":0,"data":{"aid":1,"area_id":1,"cid":"0","file_name":"xinghuo-windsurf-Setup-8.3.7-x64.exe","file_ptime":1776518646,"file_status":1,"file_id":"3409991349481503863","file_size":"109846349","pick_code":"a7xzf4ya4752uh9a4","sha1":"3B2C8734AD81FDB5BDCCD0B8129E0E01B017CE19","sp":0,"file_type":98,"object_id":"","user_id":"103019297","is_video":0}}
```
