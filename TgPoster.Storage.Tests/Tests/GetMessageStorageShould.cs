using Shared.Enums;
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

	[Fact]
	public async Task GetMessagesAsync_WithCrossPosts_ShouldReturnStatusesOrderedByCreated()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var firstAccount = await new SocialAccountBuilder(context).WithUserId(schedule.UserId).CreateAsync();
		var secondAccount = await new SocialAccountBuilder(context).WithUserId(schedule.UserId).CreateAsync();
		var firstTarget = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(firstAccount)
			.CreateAsync();
		var secondTarget = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(secondAccount)
			.CreateAsync();
		var first = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(firstTarget)
			.WithStatus(CrossPostStatus.Failed)
			.CreateAsync();
		await Task.Delay(20);
		var second = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(secondTarget)
			.WithStatus(CrossPostStatus.Published)
			.CreateAsync();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.CrossPosts.Count.ShouldBe(2);
		result.CrossPosts.Select(x => x.Id).ShouldBe([first.Id, second.Id]);
		result.CrossPosts.Select(x => x.Status).ShouldBe([CrossPostStatus.Failed, CrossPostStatus.Published]);
	}

	[Fact]
	public async Task GetMessagesAsync_WithSentStatus_ShouldReturnIsSentTrue()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(Data.Enum.MessageStatus.Send)
			.CreateAsync();

		var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.IsSent.ShouldBeTrue();
	}
}