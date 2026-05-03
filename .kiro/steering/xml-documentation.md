---
inclusion: fileMatch
fileMatchPattern: "**/*.cs"
---

# XML 文档注释规范

## 总体原则

- 注释**解释「为什么」**，而非「做了什么」（代码本身应自解释）
- 公开 API（`public`/`protected`）**必须**提供 XML 文档注释
- 内部/私有成员可选添加注释，仅当逻辑复杂时
- **禁止**注释掉的代码留在主干分支（使用 git 管理历史）
- **禁止**无意义的注释（如 `// 获取用户` 对应 `GetUser()`）

---

## XML 注释必须包含的标签

| 标签 | 适用范围 | 说明 |
|---|---|---|
| `<summary>` | 所有公开成员 | 简要描述功能，一句话 |
| `<param>` | 方法参数 | 描述每个参数的含义和约束 |
| `<returns>` | 有返回值的方法 | 描述返回值含义 |
| `<exception>` | 可能抛出异常的方法 | 列出可能抛出的异常类型 |
| `<remarks>` | 复杂类型/方法 | 额外说明、注意事项 |

### 方法注释示例

```csharp
/// <summary>
/// 根据唯一标识异步获取订单详情。
/// </summary>
/// <param name="id">要查询的订单唯一标识，不能为 <see cref="Guid.Empty"/>。</param>
/// <param name="cancellationToken">用于取消异步操作的令牌。</param>
/// <returns>
/// 成功时返回包含订单详情的 <see cref="Result{T}"/>；
/// 订单不存在时返回 <see cref="OrderErrors.NotFound"/> 错误。
/// </returns>
/// <exception cref="ArgumentException">
/// 当 <paramref name="id"/> 为 <see cref="Guid.Empty"/> 时抛出。
/// </exception>
public async Task<Result<OrderDetailDto>> GetOrderAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{ ... }
```

### 类/接口注释示例

```csharp
/// <summary>
/// 订单聚合根，封装订单生命周期的全部业务逻辑。
/// </summary>
/// <remarks>
/// 订单状态机：Pending → Confirmed → Shipped → Completed
///                       ↓
///                   Cancelled
/// </remarks>
public sealed class Order : AggregateRoot<Guid> { }

/// <summary>
/// 定义订单仓储的读写操作契约。
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// 根据 ID 异步获取订单聚合。
    /// </summary>
    /// <param name="id">订单唯一标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在时返回订单实体；否则返回 <see langword="null"/>。</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
```

### 枚举注释示例

```csharp
/// <summary>
/// 定义订单的生命周期状态。
/// </summary>
public enum OrderStatus
{
    /// <summary>订单已创建，等待确认。</summary>
    Pending = 0,

    /// <summary>订单已确认，等待发货。</summary>
    Confirmed = 1,

    /// <summary>订单已完成交付。</summary>
    Completed = 3,
}
```

---

## 内联注释规范

```csharp
// ✅ 解释「为什么」的注释
// 115 Open Platform 要求每次请求重新获取下载链接，链接有效期仅 1 小时
var downloadUrl = await _apiClient.GetTemporaryDownloadUrlAsync(fileId, cancellationToken);

// ✅ 警告性注释
// WARNING: 不要在此处调用 SaveChangesAsync，由 Pipeline Behavior 在事务中统一提交

// ✅ TODO 注释（必须包含负责人和日期）
// TODO(@zhangsan 2026-04-01): 接入分布式锁，当前实现不支持多实例部署

// ❌ 无意义注释
// 获取订单
var order = await _repository.GetByIdAsync(id, cancellationToken);

// ❌ 注释掉的代码
// var result = await _oldService.GetAsync(id);
```

---

## 特殊注释标记

| 标记 | 用途 | 格式要求 |
|---|---|---|
| `TODO` | 待办事项 | `// TODO(@作者 日期): 描述` |
| `FIXME` | 已知缺陷，需修复 | `// FIXME(@作者 日期): 描述` |
| `HACK` | 临时解决方案 | `// HACK(@作者 日期): 原因和替代方案` |
| `NOTE` | 重要说明 | `// NOTE: 描述` |
| `WARNING` | 警告，勿随意修改 | `// WARNING: 描述` |

---

## 当前项目注意事项

- 当前项目为 **ASP.NET Core 应用**，不建议对所有公开成员强制 XML 注释为构建错误
- 对外暴露的接口（`IXxxAppService`、`IXxxRepository`）应补充完整注释
- 内部 Handler 类（`internal sealed`）可仅对复杂逻辑添加注释
- 测试项目放宽文档注释要求（`SA1600` 设为 `none`）
