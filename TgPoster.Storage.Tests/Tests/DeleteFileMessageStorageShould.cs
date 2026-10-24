using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteFileMessageStorageShould : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private readonly PosterContext context;
	private readonly DeleteFileMessageStorage sut;

	public DeleteFileMessageStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new DeleteFileMessageStorage(context);
	}

	[Fact]
	public async Task ExistMessageAsync_MessageOwnedByUser_ReturnsTrue()
	{
		var message = await new MessageBuilder(context).CreateAsync();

		var exists = await sut.ExistMessageAsync(message.Id, message.Schedule.UserId, Ct);

		exists.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistMessageAsync_MessageBelongsToAnotherUser_ReturnsFalse()
	{
		var message = await new MessageBuilder(context).CreateAsync();

		var exists = await sut.ExistMessageAsync(message.Id, Guid.NewGuid(), Ct);

		exists.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistMessageAsync_MessageDoesNotExist_ReturnsFalse()
	{
		var userId = Guid.NewGuid();

		var exists = await sut.ExistMessageAsync(Guid.NewGuid(), userId, Ct);

		exists.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteFileAsync_FileExists_RemovesFile()
	{
		var messageFile = await new MessageFileBuilder(context)
			.CreateAsync(Ct);

		(await context.MessageFiles.AnyAsync(x => x.Id == messageFile.Id, Ct)).ShouldBeTrue();

		await sut.DeleteFileAsync(messageFile.MessageId, messageFile.Id, Ct);

		(await context.MessageFiles.AnyAsync(x => x.Id == messageFile.Id, Ct)).ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteFileAsync_FileMissing_DoesNothing()
	{
		var message = await new MessageBuilder(context)
			.CreateAsync();

		var beforeCount = await context.MessageFiles.CountAsync(Ct);

		await sut.DeleteFileAsync(message.Id, Guid.NewGuid(), Ct);

		var afterCount = await context.MessageFiles.CountAsync(Ct);
		afterCount.ShouldBe(beforeCount);
	}

	[Fact]
	public async Task DeleteFileAsync_FileOfAnotherMessage_DoesNotRemoveFile()
	{
		var ownMessage = await new MessageBuilder(context).CreateAsync();
		var foreignFile = await new MessageFileBuilder(context).CreateAsync(Ct);

		await sut.DeleteFileAsync(ownMessage.Id, foreignFile.Id, Ct);

		(await context.MessageFiles.AnyAsync(x => x.Id == foreignFile.Id, Ct)).ShouldBeTrue();
	}
}