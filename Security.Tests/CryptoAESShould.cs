using Security.Cryptography;
using Shouldly;

namespace Security.Tests;

/// <summary>
///     Тесты для класса CryptoAES
/// </summary>
public sealed class CryptoAesShould
{
	private readonly ICryptoAES cryptoAES;
	public CryptoAesShould()
	{
		cryptoAES = new CryptoAES();
	}
	private const string SecretKey = "1234567890123456";

	[Fact]
	public void EncryptPlainText()
	{
		const string plainText = "Тестовое сообщение для шифрования";

		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);

		encrypted.ShouldNotBeNullOrWhiteSpace();
		encrypted.ShouldNotBe(plainText);
	}

	[Fact]
	public void DecryptCipherText()
	{
		const string plainText = "Тестовое сообщение для шифрования";
		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);

		var decrypted = cryptoAES.Decrypt(SecretKey, encrypted);

		decrypted.ShouldBe(plainText);
	}

	[Fact]
	public void EncryptAndDecryptCyrillic()
	{
		const string plainText = "Привет мир! 你好世界";

		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);
		var decrypted = cryptoAES.Decrypt(SecretKey, encrypted);

		decrypted.ShouldBe(plainText);
	}

	[Fact]
	public void EncryptAndDecryptEmptyString()
	{
		const string plainText = "";

		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);
		var decrypted = cryptoAES.Decrypt(SecretKey, encrypted);

		decrypted.ShouldBe(plainText);
	}

	[Fact]
	public void EncryptAndDecryptSpecialCharacters()
	{
		const string plainText = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);
		var decrypted = cryptoAES.Decrypt(SecretKey, encrypted);

		decrypted.ShouldBe(plainText);
	}

	[Fact]
	public void EncryptAndDecryptLongText()
	{
		var plainText = string.Join("",
			Enumerable.Repeat("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ", 100));

		var encrypted = cryptoAES.Encrypt(SecretKey, plainText);
		var decrypted = cryptoAES.Decrypt(SecretKey, encrypted);

		decrypted.ShouldBe(plainText);
	}

	[Fact]
	public void ProduceDifferentCipherTextWithDifferentKeys()
	{
		const string plainText = "Тестовое сообщение";
		const string key1 = "1234567890123456";
		const string key2 = "6543210987654321";

		var encrypted1 = cryptoAES.Encrypt(key1, plainText);
		var encrypted2 = cryptoAES.Encrypt(key2, plainText);

		encrypted1.ShouldNotBe(encrypted2);
	}

	[Fact]
	public void DecryptWithWrongKeyProducesDifferentResult()
	{
		const string plainText = "Тестовое сообщение";
		const string correctKey = "1234567890123456";
		const string wrongKey = "6543210987654321";
		var encrypted = cryptoAES.Encrypt(correctKey, plainText);

		var decrypted = cryptoAES.Decrypt(wrongKey, encrypted);

		decrypted.ShouldNotBe(plainText);
	}

	[Fact]
	public void ThrowExceptionWhenDecryptingInvalidCipherText()
	{
		const string invalidCipherText = "это не валидный base64";

		Should.Throw<FormatException>(() =>
		{
			cryptoAES.Decrypt(SecretKey, invalidCipherText);
		});
	}
}