# 手机扫码授权PKCE模式

此模式适用于无后端服务的第三方客户端，使用 OAuth 2.0 + PKCE 模式授权，无需提供 AppSecret。

![](https://cdn.nlark.com/yuque/0/2025/png/35757497/1743573053666-53382bea-461d-47a6-bfbf-515effc8cb83.png)

<h2 id="WzRhM">1.获取设备码和二维码内容</h2>
使用接口返回的 data.qrcode 作为二维码的内容，在第三方客户端展示二维码，用于给115客户端扫码授权。

<h3 id="WHlNA">基本信息</h3>
**接口名称：**

设备码方式授权

**接口路径：**

POST https://passportapi.115.com/open/authDeviceCode

<h3 id="-8">请求参数</h3>
**Headers**

| 参数名称 | 参数值 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| Content-Type | application/x-www-form-urlencoded | 是 | | |

**Body**

| 参数名称 | 参数类型 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| client_id | text | 是 | | APP ID |
| code_challenge | text | 是 | THHodGWg-FZfv8XYz7QArNGIK_aVomSHPldlSOTUtkw | PKCE 相关参数，计算如下：<br/>$code_verifier = <43~128为随机字符串>;<br/>$code_challenge = url_safe(base64_encode(sha256($code_verifier)));<br/>注意 hash 的结果是二进制格式 |
| code_challenge_method |  | 是 | sha256 | 计算 code_challenge 的 hash算法，支持 md5, sha1, sha256 |

<h3 id="-9">返回数据</h3>
| 名称 | 类型 | 是否必须 | 默认值 | 备注 | 其他信息 |
| --- | --- | --- | --- | --- | --- |
| state | number | 非必须 | | | |
| code | number | 非必须 | | | |
| message | string | 非必须 | | | |
| data | object | 非必须 | | | |
| ├─ uid | string | 非必须 | | 设备码 | |
| ├─ time | number | 非必须 | | 校验用的时间戳，轮询设备码状态用到 | |
| ├─ qrcode | string | 非必须 | | 二维码内容，第三方客户端需要根据此内容生成设备二维码，提供给115客户端扫码 | |
| ├─ sign | string | 非必须 | | 校验用的签名，轮询设备码状态用到 | |
| error | string | 非必须 | | | |
| errno | number | 非必须 | | | |

<h2 id="6d33298a">2.轮询二维码状态</h2>
此为长轮询接口。注意：当二维码状态没有更新时，此接口不会立即响应，直到接口超时或者二维码状态有更新。

<h3 id="YBGLd">基本信息</h3>
**接口名称：**

轮询二维码状态

**接口路径：**

GET https://qrcodeapi.115.com/get/status/

<h3 id="-20">请求参数</h3>
**Query**

| 参数名称 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- |
| uid | 是 | | 二维码ID/设备码，从 /open/authDeviceCode 接口 data.uid 获取 |
| time | 是 | | 校验参数，从 /open/authDeviceCode 接口 data.time 获取 |
| sign | 是 | | 校验签名，从 /open/authDeviceCode 接口 data.sign 获取 |

<h3 id="-21">返回数据</h3>
| 名称 | 类型 | 是否必须 | 默认值 | 备注 | 其他信息 |
| --- | --- | --- | --- | --- | --- |
| state | number | 非必须 | | 0.二维码无效，结束轮询；1.继续轮询； | |
| code | number | 非必须 | | | |
| message | string | 非必须 | | | |
| data | object | 非必须 | | 115客户端扫码或者输入设备码后才有值 | |
| ├─ msg | string | 非必须 | | 操作提示 | |
| ├─ status | number | 非必须 | | 二维码状态；1.扫码成功，等待确认；2.确认登录/授权，结束轮询; | |
| ├─ version | string | 非必须 | | | |

<h2 id="QCCVQ">3.获取 access_token</h2>
<h3 id="HFDIt">基本信息</h3>
**接口名称：**

用设备码换取 access_token

**接口路径：**

POST https://passportapi.115.com/open/deviceCodeToToken

<h3 id="-11">请求参数</h3>
**Headers**

| 参数名称 | 参数值 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| Content-Type | application/x-www-form-urlencoded | 是 | | |

**Body**

| 参数名称 | 参数类型 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| uid | text | 是 | | 二维码ID/设备码 |
| code_verifier | text | 是 | IGKN6CJanWxCDPDhHZJrhswQdlcPBGLqExkhyujysXaQ4fJKBk_6dlPJo47s | 上一步计算 code_challenge 的原值 code_verifier |

<h3 id="-12">返回数据</h3>
| 名称 | 类型 | 是否必须 | 默认值 | 备注 | 其他信息 |
| --- | --- | --- | --- | --- | --- |
| state | number | 非必须 | | | |
| code | number | 非必须 | | | |
| message | string | 非必须 | | | |
| data | object | 非必须 | | | |
| ├─ access_token | string | 非必须 | | 用于访问资源接口的凭证 | |
| ├─ refresh_token | string | 非必须 | | 用于刷新 access_token，有效期1年 | |
| ├─ expires_in | number | 非必须 | | access_token 有效期，单位秒 | mock: 7200 |
| error | string | 非必须 | | | |
| errno | number | 非必须 | | | |

  

<h2 id="fUwxP">更新记录</h2>
| **更新时间** | **更新内容** |
| :---: | :---: |
| ** **2025年4月7日周一 | 接口 /open/authDeviceCode code_challenge 参数兼容调整，兼容 url safe |

