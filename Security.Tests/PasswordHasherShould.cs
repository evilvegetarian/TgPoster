using Security.Authentication;
using Shouldly;

namespace Security.Tests;

/// <summary>
///     Тесты для класса PasswordHasher
/// </summary>
public sealed class PasswordHasherShould
{
	private readonly IPasswordHasher passwordHasher;

	public PasswordHasherShould()
	{
		passwordHasher = new PasswordHasher();
	}

	[Fact]
	public void GenerateHashForPassword()
	{
		const string password = "MySecurePassword123!";

		var hash = passwordHasher.Generate(password);

		hash.ShouldNotBeNullOrWhiteSpace();
		hash.ShouldNotBe(password);
		hash.Length.ShouldBeGreaterThan(50);
	}

	[Fact]
	public void GenerateDifferentHashesForSamePassword()
	{
		const string password = "MySecurePassword123!";

		var hash1 = passwordHasher.Generate(password);
		var hash2 = passwordHasher.Generate(password);

		hash1.ShouldNotBe(hash2);
	}

	[Fact]
	public void VerifyCorrectPassword()
	{
		const string password = "MySecurePassword123!";
		var hash = passwordHasher.Generate(password);

		var isValid = passwordHasher.CheckPassword(password, hash);

		isValid.ShouldBeTrue();
	}

	[Fact]
	public void RejectIncorrectPassword()
	{
		const string correctPassword = "MySecurePassword123!";
		const string wrongPassword = "WrongPassword456!";
		var hash = passwordHasher.Generate(correctPassword);

		var isValid = passwordHasher.CheckPassword(wrongPassword, hash);

		isValid.ShouldBeFalse();
	}

	[Fact]
	public void HandleEmptyPassword()
	{
		const string emptyPassword = "";

		var hash = passwordHasher.Generate(emptyPassword);
		var isValid = passwordHasher.CheckPassword(emptyPassword, hash);

		hash.ShouldNotBeNullOrWhiteSpace();
		isValid.ShouldBeTrue();
	}

	[Fact]
	public void HandlePasswordWithCyrillic()
	{
		const string password = "МойПароль123!";

		var hash = passwordHasher.Generate(password);
		var isValid = passwordHasher.CheckPassword(password, hash);

		hash.ShouldNotBeNullOrWhiteSpace();
		isValid.ShouldBeTrue();
	}

	[Fact]
	public void HandlePasswordWithSpecialCharacters()
	{
		const string password = "P@ssw0rd!#$%^&*()_+-=[]{}|;':\",./<>?";

		var hash = passwordHasher.Generate(password);
		var isValid = passwordHasher.CheckPassword(password, hash);

		hash.ShouldNotBeNullOrWhiteSpace();
		isValid.ShouldBeTrue();
	}

	[Fact]
	public void HandleLongPassword()
	{
		var password = new string('a', 1000);

		var hash = passwordHasher.Generate(password);
		var isValid = passwordHasher.CheckPassword(password, hash);

		hash.ShouldNotBeNullOrWhiteSpace();
		isValid.ShouldBeTrue();
	}

	[Fact]
	public void RejectPasswordWithMinorDifference()
	{
		const string password1 = "MyPassword123";
		const string password2 = "MyPassword124";
		var hash = passwordHasher.Generate(password1);

		var isValid = passwordHasher.CheckPassword(password2, hash);

		isValid.ShouldBeFalse();
	}

	[Fact]
	public void RejectPasswordWithDifferentCase()
	{
		const string password = "MyPassword123";
		const string passwordDifferentCase = "mypassword123";
		var hash = passwordHasher.Generate(password);

		var isValid = passwordHasher.CheckPassword(passwordDifferentCase, hash);

		isValid.ShouldBeFalse();
	}
}