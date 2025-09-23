using System.Security.Cryptography;
using System.Text;

namespace LoanWorkflow.Api.Features.Auth;

public interface IAesEncryptionService
{
    string Encrypt(string plainText);
}

internal sealed class AesEncryptionService : IAesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(ExternalAuthOptions options)
    {
        if(string.IsNullOrWhiteSpace(options.AesKey) || string.IsNullOrWhiteSpace(options.AesIV))
            throw new ArgumentException("AES key/IV configuration missing");
        _key = Convert.FromBase64String(options.AesKey);
        _iv = Convert.FromBase64String(options.AesIV);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }
}
