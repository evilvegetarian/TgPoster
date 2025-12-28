using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ListOpenRouterSettingStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly ListOpenRouterSettingStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAsync_WithExist_ShouldValidReturn()
	{
		var user = await new UserBuilder(context).CreateAsync(ct);
		var setting1 = new OpenRouterSettingBuilder(context).WithUser(user).Create();
		var setting2 = new OpenRouterSettingBuilder(context).WithUser(user).Create();

		var response = await sut.GetAsync(user.Id, ct);
		response.Select(x => x.Id).ShouldContain(setting1.Id);
		response.Select(x => x.Id).ShouldContain(setting2.Id);
		response.Count.ShouldBe(2);
	}


	[Fact]
	public async Task GetAsync_WithNotExistUserId_ShouldValidReturnEmpty()
	{
		var response = await sut.GetAsync(Guid.NewGuid(), ct);
		response.Count.ShouldBe(0);
	}
}