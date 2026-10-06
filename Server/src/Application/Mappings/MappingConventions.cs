namespace Application.Mappings;

/// <summary>
/// 手动对象映射约定锚点类型。<br/>
/// 本类型仅用作命名空间标识，不承载运行时行为，业务模块无需引用。
/// </summary>
/// <remarks>
/// <para>
/// 按设计 §7. 手动对象映射 + 需求 11 的约定：
/// </para>
/// <list type="bullet">
///   <item><description>
///     Entity ↔ DTO 之间使用**静态扩展方法** <c>ToDto()</c> / <c>ToEntity()</c>，编译期可检查类型安全，不使用反射（R11.1、R11.2）。
///   </description></item>
///   <item><description>
///     映射方法按业务模块组织，例如：<br/>
///     <c>Application/CloudDrive/Mappings/CloudDriveMappings.cs</c> → <c>public static class CloudDriveMappings { public static CloudDriveDto ToDto(this CloudDriveAggregate entity) { ... } }</c>
///   </description></item>
///   <item><description>
///     严禁引入 AutoMapper 等反射型映射库（见 <c>CD0004</c>）。
///   </description></item>
/// </list>
/// <example>
/// 业务模块示例：
/// <code>
/// public static class OrderMappings
/// {
///     public static OrderDto ToDto(this Order order) => new(
///         Id: order.Id,
///         CustomerId: order.CustomerId,
///         TotalAmount: order.TotalAmount,
///         CreatedAt: order.CreatedAt);
///
///     public static Order ToEntity(this CreateOrderCommand command) => Order.Create(
///         customerId: command.CustomerId,
///         items: command.Items.Select(i => i.ToOrderItem()).ToList());
/// }
/// </code>
/// </example>
/// </remarks>
public static class MappingConventions
{
    /// <summary>
    /// 映射扩展方法推荐的静态命名。业务模块命名可参考：<c>{AggregateName}Mappings</c>。
    /// </summary>
    public const string RecommendedTypeSuffix = "Mappings";
}
