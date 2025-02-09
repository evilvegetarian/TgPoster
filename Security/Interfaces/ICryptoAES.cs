using Microsoft.Extensions.Options;

namespace Security.Interfaces;

public interface ICryptoAES
{
    string Encrypt(IOptions<BaseOptions> options, string plainText);
    string Decrypt(IOptions<BaseOptions> options, string cipherText);
}