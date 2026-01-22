namespace Security.Cryptography;

public interface ICryptoAES
{
	string Encrypt(string secretKey, string plainText);
	string Decrypt(string secretKey, string cipherText);
}