namespace Application.Abstractions;

/// <summary>当前 HTTP 请求的本地登录会话上下文。</summary>
public interface IUserSessionContextAccessor
{
    Guid? SessionId { get; }
    void SetSession(Guid sessionId);
}
