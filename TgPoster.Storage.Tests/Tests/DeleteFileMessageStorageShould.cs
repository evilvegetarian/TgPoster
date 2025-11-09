using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteFileMessageStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly DeleteFileMessageStorage sut;

	private static readonly CancellationToken Ct = CancellationToken.None;

	public DeleteFileMessageStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new DeleteFileMessageStorage(context);
	}

	[Fact]
	public async Task ExistMessageAsync_MessageOwnedByUser_ReturnsTrue()
	{
		var message = await new MessageBuilder(context).CreateAsync(Ct);

		var exists = await sut.ExistMessageAsync(message.Id, message.Schedule.UserId, Ct);

		exists.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistMessageAsync_MessageBelongsToAnotherUser_ReturnsFalse()
	{
		var message = await new MessageBuilder(context).CreateAsync(Ct);

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

		await sut.DeleteFileAsync(messageFile.Id, Ct);

		(await context.MessageFiles.AnyAsync(x => x.Id == messageFile.Id, Ct)).ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteFileAsync_FileMissing_DoesNothing()
	{
		await new MessageBuilder(context)
			.CreateAsync(Ct);

		var beforeCount = await context.MessageFiles.CountAsync(Ct);

		await sut.DeleteFileAsync(Guid.NewGuid(), Ct);

		var afterCount = await context.MessageFiles.CountAsync(Ct);
		afterCount.ShouldBe(beforeCount);
	}
}