namespace Application.CloudDrive.Dtos;

/// <summary>
/// 文档渲染方式。
/// </summary>
public enum DocRenderMode
{
    /// <summary>
    /// 前端直接渲染。
    /// </summary>
    Direct,

    /// <summary>
    /// 使用第三方预览服务渲染。
    /// </summary>
    ThirdParty,

    /// <summary>
    /// 当前不可预览。
    /// </summary>
    NotAvailable
}
