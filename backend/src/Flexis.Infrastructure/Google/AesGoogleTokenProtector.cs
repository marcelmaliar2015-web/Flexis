using System.Security.Cryptography;
using System.Text;
using Flexis.Application.Google;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Google;

internal sealed class AesGoogleTokenProtector : IGoogleTokenProtector
{
    private readonly byte[] _key;

    public AesGoogleTokenProtector(IOptions<GoogleOAuthSettings> options)
    {
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.TokenProtectionKey));
    }

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        var packed = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, packed, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, packed, nonce.Length + ciphertext.Length, tag.Length);
        return Convert.ToBase64String(packed);
    }

    public string Unprotect(string protectedText)
    {
        var packed = Convert.FromBase64String(protectedText);
        var nonce = packed.AsSpan(0, 12);
        var tag = packed.AsSpan(packed.Length - 16, 16);
        var ciphertext = packed.AsSpan(12, packed.Length - 28);
        var plaintextBytes = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
