using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class ParseChannelWorkerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly Helper helper = new(fixture.GetDbContext());
	private readonly ParseChannelWorkerStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetChannelParsingParametersAsync_ShouldReturnIdsWithCorrectStatusAndCheckNewPosts()
	{
		// Arrange
		var cpp1 = await helper.CreateChannelParsingParametersAsync(null, ParsingStatus.InHandle, true);

		var cpp2 = await helper.CreateChannelParsingParametersAsync(null, ParsingStatus.Finished, true);

		var cpp3 = await helper.CreateChannelParsingParametersAsync(null, ParsingStatus.InHandle);

		var cpp4 = await helper.CreateChannelParsingParametersAsync(null, ParsingStatus.Waiting, true);

		await context.SaveChangesAsync();

		// Act
		var result = await sut.GetChannelParsingParametersAsync();

		// Assert
		result.ShouldContain(cpp4.Id);
		result.ShouldNotContain(cpp2.Id);
		result.ShouldNotContain(cpp3.Id);
		result.ShouldNotContain(cpp1.Id);
	}

	[Fact]
	public async Task GetChannelParsingParametersAsync_ShouldReturnEmptyListIfNoMatch()
	{
		// Arrange
		var cpp = await helper.CreateChannelParsingParametersAsync(null, ParsingStatus.Waiting);

		// Act
		var result = await sut.GetChannelParsingParametersAsync();

		// Assert
		result.ShouldNotContain(cpp.Id);
	}

	[Fact]
	public async Task SetInHandleStatusAsync_ShouldUpdateStatusForGivenIds()
	{
		// Arrange
		var cpp1 = await helper.CreateChannelParsingParametersAsync();
		cpp1.Status = ParsingStatus.Waiting;
		var cpp2 = await helper.CreateChannelParsingParametersAsync();
		cpp2.Status = ParsingStatus.Finished;
		await context.SaveChangesAsync();

		// Act
		await sut.SetInHandleStatusAsync([cpp1.Id, cpp2.Id]);
		await context.Entry(cpp1).ReloadAsync();
		await context.Entry(cpp2).ReloadAsync();

		// Assert
		cpp1.Status.ShouldBe(ParsingStatus.InHandle);
		cpp2.Status.ShouldBe(ParsingStatus.InHandle);
	}

	[Fact]
	public async Task SetInHandleStatusAsync_ShouldNotThrowIfIdsNotFound()
	{
		// Arrange
		var nonExistentId = Guid.NewGuid();

		// Act & Assert
		await Should.NotThrowAsync(async () =>
		{
			await sut.SetInHandleStatusAsync([nonExistentId]);
		});
	}

	[Fact]
	public async Task SetWaitingStatusAsync_ShouldUpdateStatusForExistingId()
	{
		// Arrange
		var cpp = await helper.CreateChannelParsingParametersAsync();
		cpp.Status = ParsingStatus.InHandle;
		await context.SaveChangesAsync();

		// Act
		await sut.SetWaitingStatusAsync(cpp.Id);
		await context.Entry(cpp).ReloadAsync();

		// Assert
		cpp.Status.ShouldBe(ParsingStatus.Waiting);
	}

	[Fact]
	public async Task SetErrorStatusAsync_ShouldUpdateStatusForExistingId()
	{
		var cpp = await helper.CreateChannelParsingParametersAsync();
		cpp.Status = ParsingStatus.InHandle;
		await context.SaveChangesAsync();

		await sut.SetErrorStatusAsync(cpp.Id);

		var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == cpp.Id);
		channelParsingParameters.ShouldNotBeNull();
		channelParsingParameters.Status.ShouldBe(ParsingStatus.Failed);
	}
}