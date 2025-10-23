using System.Security.Cryptography;
using System.Text;
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

	public string Encrypt(string secretKey, string plainText)
	{
		aes.Key = Encoding.UTF8.GetBytes(secretKey);
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

	public string Decrypt(string secretKey, string cipherText)
	{
		aes.Key = Encoding.UTF8.GetBytes(secretKey);
		var cipherBytes = Convert.FromBase64String(cipherText);
		using var memoryStream = new MemoryStream(cipherBytes);
		using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
		using var reader = new StreamReader(cryptoStream);
		var plaintext = reader.ReadToEnd();

		return plaintext;
	}
}