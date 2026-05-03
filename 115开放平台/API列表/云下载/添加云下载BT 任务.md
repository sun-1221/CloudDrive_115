# 添加云下载BT 任务

<h3 id="9e5ffa06">基本信息</h3>
Path 域名 + /open/offline/add_task_bt   

Method POST   

接口描述：添加云下载链接任务  

<h3 id="527466ff">请求参数</h3>
<h3 id="head">head</h3>
| 参数名称 | 参数值 | 是否必须 | 示例 |
| --- | --- | --- | --- |
| Authorization | Bearer access_token | 是 | Bearer abcdefghijklmnopqrstuvwxyz |

<h3 id="body">**Body(form-data)**</h3>
| 参数名称 | 参数类型 | 是否必须 | 备注 |
| --- | --- | --- | --- |
| info_hash | string | 是 | BT任务hash |
| wanted | string | 是 | BT任务选中下载文件索引，半角逗号隔开 |
| save_path | string | 是 | BT任务文件保存路径 |
| torrent_sha1 | string | 是 | BT种子sha1 |
| pick_code | string | 是 | BT种子的提取码 |
| wp_path_id | string | 否 | 保存目标文件夹id |

<h3 id="1bbbb204">注意事项</h3>
wp_path_id 不传默认到根目录下面  
save_path 传的是wp_path_id所在文件夹下面的路径  
如wp_path_id不传或传云下载的文件夹ID，save_path传 A/B 最终下载的文件的路径为根目录/A/B/

<h3 id="5e33a30e">返回数据说明</h3>
| 字段 | 类型及范围 | 说明 |
| --- | --- | --- |
| state | bool | 操作结果状态值，true成功，false失败 |
| message | string | 操作返回消息，成功时空值 |
| code | int | 操作返回号码，成功时返回0 |
| data | array | 数据 |

