using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeletePublishedMessageStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DeletePublishedMessageStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetMessageAsync_WithOwnedPublishedMessage_ShouldReturnChannelAndIds()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTelegramMessageIds([10, 11, 12], 12)
			.CreateAsync();

		var result = await sut.GetMessageAsync(message.Id, schedule.UserId, Ct);

		result.ShouldNotBeNull();
		result.ScheduleId.ShouldBe(schedule.Id);
		result.ChannelId.ShouldBe(schedule.ChannelId);
		result.IsPublished.ShouldBeTrue();
		result.TelegramMessageIds.ShouldBe([10, 11, 12]);
	}

	[Fact]
	public async Task GetMessageAsync_WithAnotherUser_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.CreateAsync();

		var result = await sut.GetMessageAsync(message.Id, Guid.NewGuid(), Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteMessageAsync_ShouldSoftDeleteMessage()
	{
		var message = await new MessageBuilder(context).WithStatus(MessageStatus.Send).CreateAsync();

		await sut.DeleteMessageAsync(message.Id, Ct);

		var deleted = await context.Messages
			.IgnoreQueryFilters()
			.AsNoTracking()
			.FirstAsync(x => x.Id == message.Id);
		deleted.Deleted.ShouldNotBeNull();
	}
}
