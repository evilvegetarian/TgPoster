using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class FileStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task MarkFileAsUploadedToS3Async_WithExistingFile_ShouldSetIsInS3ToTrue()
	{
		var sut = new FileStorage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);
		messageFile.IsInS3.ShouldBeFalse();

		await sut.MarkFileAsUploadedToS3Async(messageFile.Id, CancellationToken.None);

		var updatedFile = await context.MessageFiles.FirstAsync(f => f.Id == messageFile.Id);
		updatedFile.IsInS3.ShouldBeTrue();
	}

	[Fact]
	public async Task MarkFileAsUploadedToS3Async_WithMultipleCalls_ShouldRemainTrue()
	{
		var sut = new FileStorage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);

		await sut.MarkFileAsUploadedToS3Async(messageFile.Id, CancellationToken.None);
		await sut.MarkFileAsUploadedToS3Async(messageFile.Id, CancellationToken.None);

		var updatedFile = await context.MessageFiles.FirstAsync(f => f.Id == messageFile.Id);
		updatedFile.IsInS3.ShouldBeTrue();
	}

	[Fact]
	public async Task GetFileInfoAsync_WithExistingFile_ShouldReturnFileInfo()
	{
		var sut = new FileStorage(context);
		var messageFile = await new MessageFileBuilder(context).CreateAsync(CancellationToken.None);

		var result = await sut.GetFileInfoAsync(messageFile.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TgFileId.ShouldBe(messageFile.TgFileId);
		result.ContentType.ShouldBe(messageFile.ContentType);
		result.MessageId.ShouldBe(messageFile.MessageId);
	}

	[Fact]
	public async Task GetFileInfoAsync_WithNonExistingFile_ShouldReturnNull()
	{
		var sut = new FileStorage(context);
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetFileInfoAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeNull();
	}
}
