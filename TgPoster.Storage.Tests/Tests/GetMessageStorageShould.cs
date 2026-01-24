using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly GetMessageStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetMessagesAsync_WithExistingMessage_ShouldReturnMessage()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context).WithScheduleId(schedule.Id).CreateAsync();
		var messageFile = await new MessageFileBuilder(context).WithMessageId(message.Id).CreateAsync();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(message.Id);
		result.TextMessage.ShouldBe(message.TextMessage);
		result.ScheduleId.ShouldBe(message.ScheduleId);
		//result.TimePosting.ShouldBe(message.TimePosting);
		result.Files.ShouldNotBeEmpty();
		result.Files.Count.ShouldBe(1);
		result.Files.First().Id.ShouldBe(messageFile.Id);
		result.Files.First().ContentType.ShouldBe(messageFile.ContentType);
		result.Files.First().TgFileId.ShouldBe(messageFile.TgFileId);
	}

	[Fact]
	public async Task GetMessagesAsync_WithVideoMessageFile_ShouldReturnVideoWithThumbnails()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = new MessageBuilder(context).WithScheduleId(schedule.Id).WithVideoMessageFile().Create();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Files.ShouldNotBeEmpty();
		var returnedFile = result.Files.First();
		returnedFile.ContentType.ShouldBe(FileTypes.Video.GetContentType());
		returnedFile.Previews.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task GetMessagesAsync_WithNonExistingMessage_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var nonExistingMessageId = Guid.NewGuid();

		var result = await sut.GetMessagesAsync(nonExistingMessageId, user.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetMessagesAsync_WithWrongUserId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context).WithScheduleId(schedule.Id).CreateAsync();
		var wrongUserId = Guid.NewGuid();

		var result = await sut.GetMessagesAsync(message.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetMessagesAsync_WithMessageWithoutFiles_ShouldReturnMessageWithEmptyFiles()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context).WithScheduleId(schedule.Id).CreateAsync();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(message.Id);
		result.Files.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetMessagesAsync_WithMultipleFiles_ShouldReturnAllFiles()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = new MessageBuilder(context).WithScheduleId(schedule.Id).WithPhotoMessageFile()
			.WithVideoMessageFile().Create();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Files.Count.ShouldBe(2);
		result.Files.ShouldContain(x => x.ContentType == FileTypes.Photo.GetContentType());
		result.Files.ShouldContain(x => x.ContentType == FileTypes.Video.GetContentType());
	}
}