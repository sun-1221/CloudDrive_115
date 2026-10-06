# 115 网页端脱敏接口契约

## 1. 文档范围与证据权威

本文档只记录持久化工作流 `115-web-contract-20260809-019fe56c` 已验收的 2026-08-09 脱敏采集结果，不引用任何未验收的历史采集、外部文档或第三方客户端实现。

验收入口为 `capture-result.json`（SHA-256 `1f1cfdf92f8c355e6b5979e0829e8b691d034e5a341f0bb1131395a1bfe39c60`），其四个说明性 artifact 已逐项复核：

| Artifact | SHA-256 |
|---|---|
| 总脱敏契约证据清单 | `5dfa9f316ea091961ecd4223b0d81b8203d708cd1f19a7d4671fcb41ff3f5421` |
| 会员账号脱敏契约清单 | `e8d23827a17d01f996e3463a7cd45fc5f41858ddf55e97e9e2709cc0ae94105b` |
| 云下载补采证据清单 | `ff3d569288a283f186891956dc98002ed3debcb9990999d347938f69c3f3ed8a` |
| 遗漏契约补采增量 | `8e210c5f26ad0dde9a25c9775ba83c3e21926b1f7211a5d6717f7ef5d06f7cf9` |

结构化证据由两批 NDJSON 组成，分别为 44 条与 72 条。所有原始 URL、Cookie/Set-Cookie/Authorization 值、账号标识、文件名、pickCode、下载地址、签名、金额和动态资源标识均未保存。

### 1.1 证据等级

- `collected`：真实网页请求与响应均已结构化采集，且存在成功样本。
- `low-confidence`：只取得空数据、错误样本、单样本，或只确认动作已发送。
- `ui-collected/http-not-collected`：网页场景成功，但未取得请求或响应 HTTP 契约。
- `not-collected`：没有足以定义 method/path/参数/响应的已验收证据。

除非明确写明，本文所有参数都只是 `observed`，未通过省略实验证明必填性，也不得从单样本推断枚举。

### 1.2 会话与请求头观察

- 浏览器登录会话是访问前提，系统架构使用服务端 115 Cookie 会话。
- 采集器未读取 Cookie、LocalStorage 或会话存储。
- 浏览器可见 header 名称中未观察到 `Authorization`，观察到 `Referer`。
- 当前安全证据无法反推出 Cookie 的具体值或浏览器底层附带方式。

## 2. 能力状态总表

| 能力 | 状态 | 可启用范围 |
|---|---|---|
| 文件列表 | collected | 可按本文 `/files` 契约启用 |
| 文件搜索 | not-collected | 禁用；不得沿用旧样本 |
| 上传 | not-collected | 禁用；旧未脱敏上传样本不是本轮依据 |
| 下载地址 | collected | 可按 `/files/download` 启用 |
| 新建文件夹 | collected | 可按 `/files/add` 启用 |
| 重命名 | collected | 可按 `/files/batch_rename` 启用 |
| 星标 | collected | 可按 `/files/star` 启用 |
| 复制 | collected | 可按 `/files/copy` 启用；批量能力未证明 |
| 移动、删除到回收站 | ui-collected/http-not-collected | 禁用 HTTP 能力 |
| 回收站列表 | collected | 顶层与非空 `data[]` item 可启用 |
| 回收站还原、彻底删除、清空 | ui-collected/http-not-collected | 禁用 HTTP 能力 |
| 视频播放 | collected | 播放响应可启用；清晰度/音轨不可用 |
| 字幕 | low-confidence | 仅空列表 envelope；非空 item 不可用 |
| 播放进度读取 | collected | 可按非空对象 schema 启用 |
| 播放进度写入 | not-collected | 禁用 |
| 主动转码 | low-confidence | method/path/query 已观察；JSON 结果未采集，禁用 |
| 云下载配额/计数/空列表 | collected | 可按 `clouddownload.115.com/web/` 读取契约启用 |
| 云下载 URL/BT 创建、单任务删除 | ui-collected/http-not-collected | 网页场景已验证，后端 HTTP 能力仍禁用 |
| 云下载清空 | not-collected | 未执行，禁用 |
| 我的分享 `share/slist` | collected | 非空 item 可启用 |
| 分享 send/receive | low-confidence | 只有错误契约；成功列表禁用 |
| 余额、VIP 限制、VIP 卡、空间信息 | collected | 可按各自 schema 启用 |
| 基础资料 | not-collected | 禁用；不得使用任何旧基础资料样本 |
| 优惠券 item | low-confidence | 仅空列表 envelope |

## 3. 文件与目录

### 3.1 `GET https://webapi.115.com/files` — collected

- 样本数：52 个成功样本，HTTP 200，`text/html` 中承载 JSON。
- observed query：`aid,asc,cid,count_folders,custom_order,fc_mix,format,limit,natsort,nf,o,offset,show_dir,stdir,type`。
- 已保留的非敏感 observed 控制值：`aid=1,format=json,asc=0,natsort=1,o=user_ptime,offset=0,limit∈{1,20,50,115},show_dir∈{0,1},type∈{0,4}`；其他值已脱敏。该集合不是枚举或必填规则。
- 无 request body。
- 成功 envelope：`state=true,errNo=0`。
- 顶层字段并集：`aid,cid,count,cur,data,errNo,error,fc_mix,fields,file_count,folder_count,is_asc,is_q,is_share,last_utime,limit,max_size,min_size,natsort,offset,order,page_size,path,r_all,record_open_time,star,state,stdir,suffix,sys_count,type,uid,use_cache,cost_time_1,cost_time_2,cost_time_3,cost_time_4,cost_time_5,cost_time_6,cost_time_7,cost_time_8`，另有已脱敏动态字段。
- 顶层 JSON 类型：`state:boolean`、`data/path:array`、`aid/error/fields/order/record_open_time/suffix:string`、`cid:number|string`；其余所列固定顶层字段为 number。脱敏动态字段只记录为 string，不得建立业务映射。
- `data[]` item 字段并集：`aid,c,cc,check_code,check_msg,cid,class,d,e,ec,epos,et,fatr,fc,fdes,fid,fl,fuuid,fvs,hdf,ico,is_top,ispl,issct,iv,m,n,ns,p,pc,pid,play_long,pt,q,s,score,sh,sha,sta,star_time,t,te,to,tp,tu,u,uid`。
- `data[]` JSON 类型：
  - `array`：`fl`（样本为空，item 未采集）。
  - `string`：`cc,check_msg,cid,class,e,epos,fatr,fid,ico,n,ns,pc,pid,pt,sha,t,te,tp,tu,u`。
  - `number|string`：`aid,sh`。
  - `number`：其余所列 item 字段。
- `path[]` item：`aid,cid,isp,name,pid`。
- `path[]` 所列字段类型均为 string。
- 错误样本：未采集；不得把成功 envelope 当成完整错误契约。

### 3.2 `GET https://webapi.115.com/files/cover` — low-confidence

- 1 个成功样本。
- observed query：`file_id,folder_as_file`。
- 顶层字段：`data,errno,error,state`；成功 `state=true,errno=0`。
- 单样本未建立 `data` 语义、字段必填性或错误枚举。

### 3.3 `POST https://webapi.115.com/files/add` — collected

- 场景：专用测试目录中新建文件夹；3 个成功样本。
- query：无。
- body：`application/x-www-form-urlencoded`；observed `cname,pid`。
- 响应：HTTP 200，`text/html` JSON；顶层 `aid,cid,cname,errno,error,file_id,file_name,state`。
- JSON 类型：`aid:number`、`cid/cname/errno/error/file_id/file_name:string`、`state:boolean`。
- 成功：`state=true,errno=""`。
- 错误样本：未采集。

### 3.4 `POST https://webapi.115.com/files/batch_rename` — collected

- 场景：重命名并恢复测试名称；4 个成功样本。
- query：无。
- form body：`files_new_name[{dynamic}],format`；动态资源 ID 未保存。
- 响应顶层：`data,errno,error,state`。
- JSON 类型：`data:object`（动态资源 ID → string）、`errno:number`、`error:string`、`state:boolean`。
- 成功：`state=true,errno=0`。
- 错误样本：未采集。

### 3.5 `POST https://webapi.115.com/files/star` — collected

- 场景：星标与取消星标；5 个成功样本。
- query：无。
- form body：`file_id,format,star`。
- 响应顶层：`error,state`；成功 `state=true`。
- JSON 类型：`error:string,state:boolean`。
- `star` 枚举、字段必填性与错误 schema 未建立。

### 3.6 `POST https://webapi.115.com/files/copy` — collected

- 场景：复制专用测试文件夹到专用目标目录；1 个成功样本。
- query：无。
- form body：`fid[0],format,pid`。
- 响应顶层：`errno,error,state`；成功 `state=true,errno=""`。
- JSON 类型：`errno/error:string,state:boolean`。
- 仅证明单下标样本，不得声称批量下标规则或字段必填性。

### 3.7 `GET https://webapi.115.com/files/download` — collected

- 场景：查询合成文本测试文件的下载地址，未保存本地文件；1 个成功样本。
- observed query：`_,dl,pickcode`；无 body。
- 响应顶层：`file_id,file_name,file_size,file_url_302,is_115chrome,is_snap,is_vip,msg,msg_code,pickcode,state,user_id`。
- JSON 类型：`file_id/file_name/file_size/file_url_302/msg/pickcode:string`、`is_115chrome/is_snap/is_vip/msg_code/user_id:number`、`state:boolean`。
- 成功：`state=true`。
- `_` 语义、`dl` 枚举、必填性与错误 schema 未建立。
- 下载 URL、文件名、pickCode 与账号标识不得记录或持久化。

## 4. 回收站

### 4.1 `GET https://webapi.115.com/rb` — collected

- observed query：`asc,format,limit,o,offset`；无 body。
- 响应：HTTP 200 JSON。
- 顶层：`count,data,error,is_asc,offset,order,page_size,rb_pass,state`；成功 `state=true`。
- `count` 在非空样本中的实际类型为 string，`data` 为 array。
- 非空 `data[]` item：`cid,dtime,file_name,file_size,id,parent_name,status,type`；该样本中字段值类型均为 string。
- `status/type` 枚举与字段必填性未建立。
- 还原、彻底删除和清空的 HTTP 契约未采集。

## 5. 视频、字幕与进度

### 5.1 `GET https://webapi.115.com/files/video` — collected

- observed query：`format,local,pickcode,share_id`；无 body。
- 成功顶层：`download_url,dv_profile,file_id,file_name,file_size,file_status,hdr_type,inlay_power,is_dolby_vision,is_hdr,is_mark,parent_id,pick_code,sha1,state,video_push_state`。
- JSON 类型：`download_url/dv_profile/file_id/file_name/file_size/hdr_type/is_mark/parent_id/pick_code/sha1:string`、`file_status/inlay_power/is_dolby_vision/is_hdr:number`、`state/video_push_state:boolean`。
- 成功：`state=true`。
- 早期错误样本顶层：`state,errno,error`，其中已观察 `state=false,errno=406`。
- 成功样本没有清晰度数组或音轨数组；不得从 `download_url` 或 `dv_profile` 推断清晰度枚举。
- 播放/下载 URL、文件名、pickCode、SHA1 与账号标识不得保存。

### 5.2 `GET https://webapi.115.com/files/history` — collected（只读）

- observed query：`fetch,format,pick_code`；无 body。
- 成功顶层：`data,error,state`；成功 `state=true`。
- 非空 `data`：`add_time,category,file_name,hash,pick_code,thumb,time,watch_end`。
- JSON 类型：`data:object`；`add_time/category/time/watch_end:number`；`file_name/hash/pick_code/thumb:string`；`error:string,state:boolean`。
- `time/watch_end` 的单位、枚举与字段必填性未通过多样本实验确认。
- `POST /files/history` 写入契约未包含在已验收证据中，保持 disabled。

### 5.3 `GET https://webapi.115.com/movies/subtitle` — low-confidence

- observed query：`format,pickcode`；无 body。
- 成功顶层：`data,errNo,error,state`；成功 `state=true,errNo=0`。
- `data.autoload` 与 `data.list` 都是 array。
- 已追加抽样 10 个真实视频，均为空数组；非空 subtitle item 仍为 not-collected。

### 5.4 `GET https://webapi.115.com/files/video_push` — low-confidence

- observed query：`format,op,pickcode`。
- 已分别发送一次 `op=vip_push` 与 `op=pay_push`，均返回非 JSON 文本；正文未保存，也未重试。
- method/path/query 的发送事实可信；HTTP 状态、成功/错误字段与费用结果均为 not-collected，因此后端不得启用主动转码。

## 6. 分享

### 6.1 `GET https://webapi.115.com/share/slist` — collected

- observed query：`asc,format,limit,offset,order,show_cancel_share`；无 body。
- 顶层：`bind_receive_code,count,define_receive_code,errno,error,list,state`；成功 `state=true,errno=0`。
- 非空 `list[]` item：`access_user_ids,access_user_nums,auto_fill_recvcode,auto_renewal,can_appeal,can_global_appeal,can_share_appeal,create_time,file_category,file_count,file_name_state,file_size,file_state,folder_count,receive_code,receive_count,receive_user_ids,receive_user_limit,receive_user_nums,renewal_time,sha1,share_code,share_duration,share_ex_time,share_state,share_state_text,share_title,share_url,skip_login_down_flow,skip_login_down_flow_limit,skip_login_state,snap_id,user_id`。
- number 类型：`access_user_nums,auto_fill_recvcode,auto_renewal,can_appeal,can_global_appeal,can_share_appeal,create_time,file_category,file_name_state,file_size,file_state,receive_count,receive_user_nums,share_ex_time,share_state`；其余所列 item 字段在样本中为 string。
- 分享码、访问码、分享 URL、文件名与账号标识不得保存或写入日志。
- 单样本不建立状态枚举、业务语义或必填性。

### 6.2 `GET https://webapi.115.com/share/send` 与 `/share/receive` — low-confidence

- 每个端点 2 个真实错误样本。
- observed query：`format,limit,offset`；无 body。
- 错误顶层：`errNo,errno,error,request,state`。
- 已观察：`state=false,errNo=980005,errno=980005`。
- 成功 schema 与非空 item 为 not-collected；对应前端 Tab 必须保持 disabled。

## 7. 云下载

统一实际入口为 `POST https://clouddownload.115.com/web/`，通过 query `ac` 选择操作；响应为 HTTP 200、`text/plain` JSON。不得继续使用旧 `115.com/web/lixian/` 路径。

### 7.1 `ac=get_quota_package_info` — collected

- query：`ac,uid`；无 body；4 个样本。
- 顶层：`count,max_size,package,surplus,used`。
- JSON 类型：`count/max_size/surplus/used:number`、`package:object`。`package` 动态键 item 为 `{ count:number,expire_info:null|array,name:string,surplus:number,used:number }`；非空 `expire_info[]` item 为 `{ expire_time:number,surplus:number }`。
- 字段必填性和 `package` 内部业务语义未建立。

### 7.2 `ac=task_count` — collected

- query：`ac`；无 body；2 个样本。
- 顶层：`downloading_count,errcode,errtype,failed_count,finished_count,state,total_count`。
- JSON 类型：除 `errtype:string` 与 `state:boolean` 外，其余所列字段均为 number。
- 成功：`state=true,errcode=0,errtype="suc"`。

### 7.3 `ac=task_lists` — collected（空列表 envelope）

- query：`ac,page,page_size,stat`；无 body；2 个空任务样本。非敏感 observed 控制值为 `ac=task_lists,page=1,page_size=15,stat=12`，不据此声明枚举。
- 顶层：`count,errcode,errtype,page,page_count,page_row,page_size,quota,real_state,state,tasks,total`。
- JSON 类型：`errtype:string`、`real_state/state:boolean`、`tasks:null`，其余所列字段均为 number。
- 成功：`state=true,errcode=0,errtype="suc"`。
- 非空 API `tasks[]` item 为 not-collected；UI 行结构不能用于猜测 item 字段。

### 7.4 `ac=add_task_urls` — low-confidence

- query：`ac`。
- form body observed：`savepath,sign,time,uid,url[0],wp_path_id`。
- 2 个重复/错误样本：顶层 `errcode,errno,error_msg,result,state`；已观察 `state=false,errcode=10010,errno=0`。
- JSON 类型：`errcode/errno:number`、`error_msg:string`、`result:null`、`state:boolean`。
- 网页 UI 已通过任务行数差值确认 URL 创建成功并单独删除，但成功 HTTP body 未暴露；成功响应 schema仍为 not-collected。
- `sign,time,uid` 的值、生成算法与必填性未采集。

### 7.5 BT、单任务删除与清空

- BT 解析、BT 创建、URL/BT 单任务删除：网页场景成功，HTTP method/path/body/response 为 not-collected。
- 非空任务列表只取得 UI 行，不得推断 API item schema。
- 页面存在非本轮任务，未执行“清空全部”；清空接口和结果均为 not-collected。

## 8. 会员、余额与空间

### 8.1 `GET https://pay.115.com/?ct=balance&ac=getBalance` — collected

- query：`ct,ac`；无 body。
- 顶层：`code,data,msg,state`；成功 `state=true,code=0`。
- `data`：`can_withdraw:string,cant_withdraw:string,detail:number,feedback_act_money:number,money:string`。
- 所有金额值不得保存或记录。

### 8.2 `GET https://vip.115.com/?ct=order&ac=get_yun_card` — collected

- query：`ct,ac`；无 body。
- 顶层：`data,state`；成功 `state=true`。
- `data`：`00_balance:number,forever_count:number,num:number,space_count:number,sqcode_cancel_count:string,sqcode_count:string,url:string,vip_count:number`。
- `00_balance` 只是上游字段名，业务语义与单位未确认。

### 8.3 `GET https://webapi.115.com/user/vip_limit?feature=2` — collected

- query：`feature`；无 body。
- 顶层：`code,data,message,state`；成功 `state=true`，`code` 实际类型为 string。
- `data.user_limit` 与 `data.limit_list[]`：`feature,level,level_name,limit_numbers`，样本类型均为 string。
- `data.vip_list` 为 object；数字键和等级语义未建立。

### 8.4 `GET https://webapi.115.com/files/index_info?count_space_nums=1` — collected

- query：`count_space_nums`；无 body。
- 顶层：`code,data,error,state`；成功 `state=true,code=0`。
- `data.space_info`：`all_remain,all_total,all_use`，各含 `size:number,size_format:string`。
- `data.count_space_nums.type_nums`：动态分类键 → `{ count:number }`。
- `data.count_space_nums.type_summury`：动态分类键 → `{ count:number,pecent:number,size:number,size_format:string }`。
- 保留上游拼写 `type_summury`、`pecent`，不得自行更名后冒充上游字段。

### 8.5 `GET https://vip.115.com/?ct=product&ac=product_list` — collected

- query：`ct,ac`；无 body。
- 顶层：`code,data,msg,order_key,price_space2,show_service_desc,state`；成功 `state=true,code=0`。
- `data`、`data[].products`、部分 `price_space2.*` 与 `show_service_desc` 为非空。
- 产品代码、价格档位与权益枚举只来自单账号单样本，不建立固定枚举。

### 8.6 `GET https://vip.115.com/?ct=act_coupon&ac=get_coupon_yhq_list` — low-confidence

- query：`ct,ac,page,page_size,status`；无 body。
- 顶层：`code,data,state`；成功 `state=true,code=0`。
- 只有空列表 envelope；优惠券 item 为 not-collected。

### 8.7 `GET https://webapi.115.com/user/setting?format=json` — collected（非基础资料）

- query：`format`；无 body。
- 顶层：`data,errno,error,state`；成功 `state=true,errno=0`。
- `data` 含播放、字幕、排序、界面和协议开关字段；值未保存。
- 该端点是账号设置，不能作为昵称、头像、UID、手机号或邮箱等基础资料来源。

## 9. 已验收 envelope / 错误字段映射

下表只定义字段选择和已观察 JSON 类型，不把“仅有成功样本”扩展成完整错误枚举。Gateway 规则键必须包含 method + host + 精确 path；`vip.115.com/` 与 `clouddownload.115.com/web/` 还必须包含 `ct/ac` 控制 query。

| # | 请求键 | `state` | 数字/字符串码字段 | 消息字段 | 证据边界 |
|---:|---|---|---|---|---|
| 1 | `GET webapi.115.com/files` | boolean | `errNo:number` | `error:string` | 仅成功 `errNo=0` |
| 2 | `POST webapi.115.com/files/add` | boolean | `errno:string` | `error:string` | 仅成功空串 |
| 3 | `POST webapi.115.com/files/batch_rename` | boolean | `errno:number` | `error:string` | 仅成功 0 |
| 4 | `POST webapi.115.com/files/copy` | boolean | `errno:string` | `error:string` | 仅成功空串 |
| 5 | `POST webapi.115.com/files/star` | boolean | 无 | `error:string` | 仅成功样本 |
| 6 | `GET webapi.115.com/files/download` | boolean | `msg_code:number` | `msg:string` | 错误枚举未建立 |
| 7 | `GET webapi.115.com/files/video` | boolean | `errno:number` | `error:string` | 已观察错误 406 |
| 8 | `GET webapi.115.com/movies/subtitle` | boolean | `errNo:number` | `error:string` | 成功空 item |
| 9 | `GET webapi.115.com/rb` | boolean | 无 | `error:string` | 错误枚举未建立 |
| 10 | `GET webapi.115.com/share/slist` | boolean | `errno:number` | `error:string` | 仅成功 0 |
| 11 | `GET webapi.115.com/share/send|receive` | boolean | `errNo:number` + `errno:number` | `error:string` | 已观察错误 980005；成功禁用 |
| 12 | `GET webapi.115.com/user/vip_limit?feature=2` | boolean | `code:string` | `message:string` | 仅成功样本 |
| 13 | `GET webapi.115.com/files/index_info?count_space_nums=1` | boolean | `code:number` | `error:string` | 仅成功 0 |
| 14 | `GET webapi.115.com/user/setting?format=json` | boolean | `errno:number` | `error:string` | 仅成功 0 |
| 15 | `GET pay.115.com/?ct=balance&ac=getBalance` | boolean | `code:number` | `msg:string` | 仅成功 0 |
| 16 | `GET vip.115.com/?ct=order&ac=get_yun_card` | boolean | 无 | 无 | 仅成功样本 |
| 17 | `GET vip.115.com/?ct=product&ac=product_list` | boolean | `code:number` | `msg:string` | 仅成功 0 |
| 18 | `GET vip.115.com/?ct=act_coupon&ac=get_coupon_yhq_list` | boolean | `code:number` | 未采集 | 成功空 item |
| 19 | `POST clouddownload.115.com/web/?ac=...` | boolean | `errcode:number`；`add_task_urls` 另有 `errno:number` | `errtype:string` 或 `error_msg:string` | 必须按 `ac` 分规则 |
| 20 | `GET webapi.115.com/files/history` | boolean | 无 | `error:string` | 成功对象/早期空错误样本 |

未知失败形态必须 fail-closed 为 `api_error` 并输出不含请求/响应体的脱敏 Warning；不得使用猜测性通用码字段。

## 10. 明确禁用清单

以下能力缺少已验收 HTTP 成功契约；后端必须返回稳定的 disabled/not-collected 语义，前端入口必须禁用，相关实现任务不得标记完成：

- `GET /files/search` 搜索契约。
- 上传初始化与 OSS 直传链路。
- 文件详情专用端点。
- 文件移动、删除到回收站。
- 回收站还原、彻底删除、清空。
- `POST /files/history` 写入播放进度。
- `files/video_push` / `movies/transcode` 的 JSON 成功与错误结果。
- 字幕非空 item、视频清晰度列表、音轨列表。
- 云下载 URL/BT 创建成功 HTTP schema、BT 解析、单任务删除、清空、签名算法、非空任务 item。
- `share/send`、`share/receive` 成功 schema 与非空 item。
- 用户基础资料、优惠券非空 item。

网页 UI 已成功执行的动作只证明场景存在，不构成上述 HTTP 契约。任何后续启用都必须先产生新的脱敏证据并更新本文档。
