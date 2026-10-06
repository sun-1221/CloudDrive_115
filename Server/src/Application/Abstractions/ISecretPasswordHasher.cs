namespace Application.Abstractions;

/// <summary>密码哈希与验证抽象。</summary>
public interface ISecretPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
