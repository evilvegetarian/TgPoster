using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UploadFileToS3StorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task GetFileInfoAsync_WithExistingFile_ShouldReturnFileInfo()
	{
		var sut = new UploadFileToS3Storage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);

		var result = await sut.GetFileInfoAsync(messageFile.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TgFileId.ShouldBe(messageFile.TgFileId);
		result.ContentType.ShouldBe(messageFile.ContentType);
		result.IsInS3.ShouldBe(messageFile.IsInS3);
	}

	[Fact]
	public async Task GetFileInfoAsync_WithNonExistingFile_ShouldReturnNull()
	{
		var sut = new UploadFileToS3Storage(context);
		var nonExistingFileId = Guid.NewGuid();

		var result = await sut.GetFileInfoAsync(nonExistingFileId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task MarkFileAsUploadedToS3Async_ShouldSetIsInS3ToTrue()
	{
		var sut = new UploadFileToS3Storage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);

		await sut.MarkFileAsUploadedToS3Async(messageFile.Id, CancellationToken.None);

		var updatedFile = await context.MessageFiles.FirstAsync(f => f.Id == messageFile.Id);
		updatedFile.IsInS3.ShouldBeTrue();
	}

	[Fact]
	public async Task GetScheduleIdByFileIdAsync_ShouldReturnCorrectScheduleId()
	{
		var sut = new UploadFileToS3Storage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);
		var message = await context.Messages.FirstAsync(m => m.Id == messageFile.MessageId);

		var result = await sut.GetScheduleIdByFileIdAsync(messageFile.Id, CancellationToken.None);

		result.ShouldBe(message.ScheduleId);
	}
}
