# 刷新 access_token

<h2 id="ve54x">刷新 access_token</h2>
<h3 id="lFfVH">基本信息</h3>
**接口名称：**

刷新 access_token

**接口路径：**

POST https://passportapi.115.com/open/refreshToken

<h3 id="wHdY2">备注</h3>
请勿频繁刷新，否则列入频控。

<h3 id="lfk76">请求参数</h3>
**Headers**

| 参数名称 | 参数值 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| Content-Type | application/x-www-form-urlencoded | 是 | | |

**Body**

| 参数名称 | 参数类型 | 是否必须 | 示例 | 备注 |
| --- | --- | --- | --- | --- |
| refresh_token | text | 是 | | 刷新的凭证 |

<h3 id="-15">返回数据</h3>
| 名称 | 类型 | 是否必须 | 默认值 | 备注 | 其他信息 |
| --- | --- | --- | --- | --- | --- |
| state | number | 非必须 | | | |
| code | number | 非必须 | | | |
| message | string | 非必须 | | | |
| data | object | 非必须 | | | |
| ├─ access_token | string | 非必须 | | **新的** access_token，同时刷新有效期 | |
| ├─ refresh_token | string | 非必须 | | **新的** refresh_token，**有效期**不延长不改变 | |
| ├─ expires_in | number | 非必须 | | access_token 有效期，单位秒 | mock: 2592000 |
| error | string | 非必须 | | | |
| errno | number | 非必须 | | | |

