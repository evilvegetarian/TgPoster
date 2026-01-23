using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class CreateTelegramSessionStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CreateTelegramSessionStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task CreateAsync_WithValidData_ShouldCreateSession()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var apiId = "123456";
		var apiHash = "test_hash_12345";
		var phoneNumber = "+79991234567";
		var name = "Test Session";

		var result = await sut.CreateAsync(
			user.Id,
			apiId,
			apiHash,
			phoneNumber,
			name,
			CancellationToken.None);

		result.Id.ShouldNotBe(Guid.Empty);
		result.Name.ShouldBe(name);
		result.IsActive.ShouldBeTrue();

		var session = await context.TelegramSessions.FindAsync(result.Id);
		session.ShouldNotBeNull();
		session.ApiId.ShouldBe(apiId);
		session.ApiHash.ShouldBe(apiHash);
		session.PhoneNumber.ShouldBe(phoneNumber);
		session.Name.ShouldBe(name);
		session.UserId.ShouldBe(user.Id);
		session.IsActive.ShouldBeTrue();
	}

	[Fact]
	public async Task CreateAsync_WithoutName_ShouldUsePhoneNumberAsName()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var phoneNumber = "+79991234567";

		var result = await sut.CreateAsync(
			user.Id,
			"123456",
			"test_hash",
			phoneNumber,
			null,
			CancellationToken.None);

		result.Name.ShouldBe(phoneNumber);
	}

	[Fact]
	public async Task CreateAsync_ShouldSetDefaultIsActiveToTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.CreateAsync(
			user.Id,
			"123456",
			"test_hash",
			"+79991234567",
			"Test",
			CancellationToken.None);

		result.IsActive.ShouldBeTrue();

		var session = await context.TelegramSessions.FindAsync(result.Id);
		session!.IsActive.ShouldBeTrue();
	}
}
