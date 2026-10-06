using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Application.CloudDrive.Auth;

using Microsoft.IdentityModel.Tokens;

namespace WebApi.Extensions;

public interface IJwtTokenFactory
{
    string Create(AuthSessionDto session);
}

internal sealed class JwtTokenFactory(IConfiguration configuration) : IJwtTokenFactory
{
    public string Create(AuthSessionDto session)
    {
        IConfigurationSection jwt = configuration.GetSection("Jwt");
        string issuer = jwt["Issuer"] ?? throw new InvalidOperationException("JWT Issuer 未配置。");
        string audience = jwt["Audience"] ?? throw new InvalidOperationException("JWT Audience 未配置。");
        string signingKey = jwt["SigningKey"] ?? throw new InvalidOperationException("JWT SigningKey 未配置。");
        DateTime now = DateTime.UtcNow;
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, session.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new("sid", session.SessionId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        ];
        JwtSecurityToken token = new(issuer, audience, claims, now, session.ExpiresAt,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
