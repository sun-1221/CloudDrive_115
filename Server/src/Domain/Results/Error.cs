namespace Domain.Results;

/// <summary>
/// 领域/应用层错误的统一表示。使用 <c>Code</c> 进行机读分类，<c>Description</c> 承载人类可读信息。
/// </summary>
/// <param name="Code">错误代码，建议采用 <c>模块.错误名</c> 形式（如 <c>Order.NotFound</c>）。</param>
/// <param name="Description">错误描述，应以中文给出上下文可读信息。</param>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// 表示"无错误"的哨兵值。仅用于成功结果场景，业务代码不应主动使用。
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
