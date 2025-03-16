namespace Security.Interfaces;

public interface ICryptoAES
{
    string Encrypt(string secretKey, string plainText);
    string Decrypt(string secretKey, string cipherText);
}