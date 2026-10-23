using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.RetryMessages;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class RetryMessagesStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly RetryMessagesStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetMessagesAsync_WithOwnedMessages_ShouldReturnInfo()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var failed = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Error)
			.CreateAsync();
		var sent = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.CreateAsync();

		var result = await sut.GetMessagesAsync([failed.Id, sent.Id], user.Id, CancellationToken.None);

		result.Count.ShouldBe(2);
		var failedInfo = result.Single(x => x.Id == failed.Id);
		failedInfo.ScheduleId.ShouldBe(schedule.Id);
		failedInfo.IsFailed.ShouldBeTrue();
		failedInfo.TimePosting.ShouldBe(failed.TimePosting, TimeSpan.FromMilliseconds(1));
		result.Single(x => x.Id == sent.Id).IsFailed.ShouldBeFalse();
	}

	[Fact]
	public async Task GetMessagesAsync_WithAnotherUserMessages_ShouldNotReturnThem()
	{
		var owner = await new UserBuilder(context).CreateAsync();
		var stranger = await new UserBuilder(context).CreateAsync();
		var ownerSchedule = await new ScheduleBuilder(context).WithUserId(owner.Id).CreateAsync();
		var strangerSchedule = await new ScheduleBuilder(context).WithUserId(stranger.Id).CreateAsync();
		var ownerMessage = await new MessageBuilder(context)
			.WithSchedule(ownerSchedule)
			.WithStatus(MessageStatus.Error)
			.CreateAsync();
		var strangerMessage = await new MessageBuilder(context)
			.WithSchedule(strangerSchedule)
			.WithStatus(MessageStatus.Error)
			.CreateAsync();

		var result = await sut.GetMessagesAsync(
			[ownerMessage.Id, strangerMessage.Id],
			stranger.Id,
			CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([strangerMessage.Id]);
	}

	[Fact]
	public async Task GetOccupiedTimesAsync_ShouldReturnRegisterAndInHandleAfterFrom()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var otherSchedule = await new ScheduleBuilder(context).CreateAsync();
		var from = DateTimeOffset.UtcNow;
		var register = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(from.AddHours(1))
			.CreateAsync();
		var inHandle = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.InHandle)
			.WithTimeMessage(from.AddHours(2))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(from.AddHours(-1))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Error)
			.WithTimeMessage(from.AddHours(3))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTimeMessage(from.AddHours(4))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(otherSchedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(from.AddHours(5))
			.CreateAsync();

		var result = await sut.GetOccupiedTimesAsync(schedule.Id, from, CancellationToken.None);

		result.Count.ShouldBe(2);
		result.ShouldContain(x => Math.Abs((x - register.TimePosting).TotalMilliseconds) < 1);
		result.ShouldContain(x => Math.Abs((x - inHandle.TimePosting).TotalMilliseconds) < 1);
	}

	[Fact]
	public async Task RescheduleAsync_ShouldReturnMessageToQueueWithoutTouchingVerification()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Error)
			.WithSendError("403: Forbidden: bot was kicked")
			.WithTelegramMessageId(42)
			.CreateAsync();
		message.IsVerified = false;
		await context.SaveChangesAsync();
		var timePosting = DateTimeOffset.UtcNow.AddMinutes(5);

		await sut.RescheduleAsync(
			[new RetriedMessage { Id = message.Id, TimePosting = timePosting }],
			CancellationToken.None);

		var updated = await context.Messages
			.AsNoTracking()
			.FirstAsync(x => x.Id == message.Id);
		updated.Status.ShouldBe(MessageStatus.Register);
		updated.TimePosting.ShouldBe(timePosting, TimeSpan.FromMilliseconds(1));
		updated.SendError.ShouldBeNull();
		updated.TelegramMessageId.ShouldBeNull();
		updated.IsVerified.ShouldBeFalse();
	}
}
