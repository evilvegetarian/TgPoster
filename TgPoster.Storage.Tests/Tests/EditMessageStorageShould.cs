using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class EditMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly EditMessageStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetMessageAsync_WithPublishedMessage_ShouldReturnTelegramStateAndTopLevelFiles()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		schedule.SignatureEnabled = true;
		schedule.SignatureFooter = "Подпись";
		await context.SaveChangesAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTextMessage("текст")
			.WithPhotoMessageFile()
			.WithVideoMessageFile()
			.WithTelegramMessageIds([10, 11], 10)
			.CreateAsync();
		var topLevelFiles = message.MessageFiles
			.Where(x => x.ParentFileId == null)
			.OrderBy(x => x.Order)
			.Select(x => x.Id)
			.ToList();

		var result = await sut.GetMessageAsync(message.Id, schedule.UserId, Ct);

		result.ShouldNotBeNull();
		result.ScheduleId.ShouldBe(schedule.Id);
		result.ChannelId.ShouldBe(schedule.ChannelId);
		result.IsPublished.ShouldBeTrue();
		result.Text.ShouldBe("текст");
		result.Signature.ShouldBe("Подпись");
		result.FileIds.ShouldBe(topLevelFiles);
		result.TelegramMessageIds.ShouldBe([10, 11]);
		result.TelegramTextMessageId.ShouldBe(10);
	}

	[Fact]
	public async Task GetMessageAsync_WithDisabledSignature_ShouldReturnNullSignature()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		schedule.SignatureEnabled = false;
		schedule.SignatureFooter = "Подпись";
		await context.SaveChangesAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.CreateAsync();

		var result = await sut.GetMessageAsync(message.Id, schedule.UserId, Ct);

		result.ShouldNotBeNull();
		result.IsPublished.ShouldBeFalse();
		result.Signature.ShouldBeNull();
	}

	[Fact]
	public async Task GetMessageAsync_WithAnotherUser_ShouldReturnNull()
	{
		var message = await new MessageBuilder(context).CreateAsync();

		var result = await sut.GetMessageAsync(message.Id, Guid.NewGuid(), Ct);

		result.ShouldBeNull();
	}
}
