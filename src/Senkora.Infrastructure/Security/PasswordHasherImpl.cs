using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.Security;

/// <summary>IPasswordHasher implementasyonu — BCrypt kullanir</summary>
public sealed class PasswordHasherImpl : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
