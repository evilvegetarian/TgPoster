using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ParseChannelWorkerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ParseChannelWorkerStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetChannelParsingParametersAsync_ShouldReturnIdsWithCorrectStatusAndCheckNewPosts()
	{
		
		var cpp1 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.InHandle).WithCheckNewPosts(true).CreateAsync();

		var cpp2 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Finished).WithCheckNewPosts(true).CreateAsync();

		var cpp3 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.InHandle).CreateAsync();

		var cpp4 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Waiting).WithCheckNewPosts(true).CreateAsync();

		await context.SaveChangesAsync();

		
		var result = await sut.GetChannelParsingParametersAsync();

		
		result.ShouldContain(cpp4.Id);
		result.ShouldNotContain(cpp2.Id);
		result.ShouldNotContain(cpp3.Id);
		result.ShouldNotContain(cpp1.Id);
	}

	[Fact]
	public async Task GetChannelParsingParametersAsync_ShouldReturnEmptyListIfNoMatch()
	{
		var cpp = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Canceled).CreateAsync();

		var result = await sut.GetChannelParsingParametersAsync();

		result.ShouldNotContain(cpp.Id);
	}

	[Fact]
	public async Task SetInHandleStatusAsync_ShouldUpdateStatusForGivenIds()
	{
		var cpp1 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Waiting).CreateAsync();
		var cpp2 = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Finished).CreateAsync();
		
		await sut.SetInHandleStatusAsync([cpp1.Id, cpp2.Id]);
		await context.Entry(cpp1).ReloadAsync();
		await context.Entry(cpp2).ReloadAsync();

		cpp1.Status.ShouldBe(ParsingStatus.InHandle);
		cpp2.Status.ShouldBe(ParsingStatus.InHandle);
	}

	[Fact]
	public async Task SetInHandleStatusAsync_ShouldNotThrowIfIdsNotFound()
	{
		
		var nonExistentId = Guid.NewGuid();

		await Should.NotThrowAsync(async () =>
		{
			await sut.SetInHandleStatusAsync([nonExistentId]);
		});
	}

	[Fact]
	public async Task SetWaitingStatusAsync_ShouldUpdateStatusForExistingId()
	{
		
		var cpp = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.InHandle).CreateAsync();

		await sut.SetWaitingStatusAsync(cpp.Id);
		await context.Entry(cpp).ReloadAsync();

		cpp.Status.ShouldBe(ParsingStatus.Waiting);
	}

	[Fact]
	public async Task SetErrorStatusAsync_ShouldUpdateStatusForExistingId()
	{
		var cpp = await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.InHandle).CreateAsync();

		await sut.SetErrorStatusAsync(cpp.Id);
		context.ChangeTracker.Clear();
		var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == cpp.Id);
		channelParsingParameters.ShouldNotBeNull();
		channelParsingParameters.Status.ShouldBe(ParsingStatus.Failed);
	}
}