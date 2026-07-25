using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Infrastructure.Security;

public sealed class EncryptionService(IConfiguration config) : IEncryptionService
{
    private readonly byte[] _key = GetKey(config["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption:Key is not configured."));

    private static byte[] GetKey(string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256-bit).");
        return key;
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);
        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullBytes.Length - iv.Length];
        Array.Copy(fullBytes, 0, iv, 0, iv.Length);
        Array.Copy(fullBytes, iv.Length, cipher, 0, cipher.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
