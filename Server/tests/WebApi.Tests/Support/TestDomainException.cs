using Domain.Exceptions;

namespace WebApi.Tests.Support;

/// <summary>
/// 测试专用的 <see cref="DomainException"/> 派生类型，允许在属性测试中任意指定状态码。
/// </summary>
public sealed class TestDomainException(int statusCode, string message, string? errorCode = null)
    : DomainException(statusCode, message, errorCode)
{
}
