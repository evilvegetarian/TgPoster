using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ListParseChannelsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ListParseChannelsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetChannelAsync_WithNonExistingUserId_ShouldReturnEmptyList()
	{
		var result = await sut.GetChannelParsingParametersAsync(Guid.NewGuid(), CancellationToken.None);
		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetChannelAsync_WithExistingUserId_ShouldReturnList()
	{
		var settings = await new ChannelParsingSettingBuilder(context).CreateAsync();

		var result = await sut.GetChannelParsingParametersAsync(settings.Schedule.UserId, CancellationToken.None);
		result.Count.ShouldBe(1);
	}
}