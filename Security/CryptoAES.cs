using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Security.Interfaces;

namespace Security;

public class CryptoAES : ICryptoAES
{
    private readonly Aes aes;

    public CryptoAES()
    {
        aes = Aes.Create();
        aes.IV = new byte[16];
    }

    public string Encrypt(IOptions<BaseOptions> options, string plainText)
    {
        aes.Key = Encoding.UTF8.GetBytes(options.Value.SecretKey);
        byte[] encrypted;
        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                }

                encrypted = memoryStream.ToArray();
            }
        }

        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(IOptions<BaseOptions> options, string cipherText)
    {
        aes.Key = Encoding.UTF8.GetBytes(options.Value.SecretKey);
        var cipherBytes = Convert.FromBase64String(cipherText);
        using var memoryStream = new MemoryStream(cipherBytes);
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        var plaintext = reader.ReadToEnd();

        return plaintext;
    }
}

public abstract class BaseOptions
{
    public abstract string SecretKey { get; set; }
}