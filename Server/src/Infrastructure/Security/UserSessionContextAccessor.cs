using Application.Abstractions;

namespace Infrastructure.Security;

internal sealed class UserSessionContextAccessor : IUserSessionContextAccessor
{
    public Guid? SessionId { get; private set; }

    public void SetSession(Guid sessionId) => SessionId = sessionId;
}
