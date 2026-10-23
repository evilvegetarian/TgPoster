using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ApproveMessagesStorageShould : IClassFixture<StorageTestFixture>
{
	private static readonly Dictionary<Guid, DateTimeOffset> NoTimeChanges = [];

	private readonly PosterContext context;
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly ApproveMessagesStorage sut;

	public ApproveMessagesStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new ApproveMessagesStorage(context);
	}

	[Fact]
	public async Task GetMessagesAsync_WithOwnedMessages_ShouldReturnInfo()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var pending = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.WithIsVerified(false)
			.CreateAsync();
		var sent = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithIsVerified(true)
			.CreateAsync();

		var result = await sut.GetMessagesAsync([pending.Id, sent.Id], schedule.UserId, ct);

		result.Count.ShouldBe(2);
		var pendingInfo = result.Single(x => x.Id == pending.Id);
		pendingInfo.ScheduleId.ShouldBe(schedule.Id);
		pendingInfo.IsVerified.ShouldBeFalse();
		pendingInfo.IsRegistered.ShouldBeTrue();
		pendingInfo.TimePosting.ShouldBe(pending.TimePosting, TimeSpan.FromMilliseconds(1));
		var sentInfo = result.Single(x => x.Id == sent.Id);
		sentInfo.IsVerified.ShouldBeTrue();
		sentInfo.IsRegistered.ShouldBeFalse();
	}

	[Fact]
	public async Task GetMessagesAsync_WithAnotherUserMessages_ShouldNotReturnThem()
	{
		var ownerSchedule = await new ScheduleBuilder(context).CreateAsync();
		var strangerSchedule = await new ScheduleBuilder(context).CreateAsync();
		var ownerMessage = await new MessageBuilder(context).WithSchedule(ownerSchedule).CreateAsync();
		var strangerMessage = await new MessageBuilder(context).WithSchedule(strangerSchedule).CreateAsync();

		var result = await sut.GetMessagesAsync([ownerMessage.Id, strangerMessage.Id], strangerSchedule.UserId, ct);

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
			.WithSchedule(otherSchedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(from.AddHours(4))
			.CreateAsync();

		var result = await sut.GetOccupiedTimesAsync(schedule.Id, from, ct);

		result.Count.ShouldBe(2);
		result.ShouldContain(x => Math.Abs((x - register.TimePosting).TotalMilliseconds) < 1);
		result.ShouldContain(x => Math.Abs((x - inHandle.TimePosting).TotalMilliseconds) < 1);
	}

	[Fact]
	public async Task ApproveMessage_WithExistingUnverifiedMessages_ShouldSetIsVerifiedToTrue()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var messageIds = new List<Message>
		{
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync()
		}.Select(x => x.Id).ToList();

		await sut.ApproveMessage(messageIds, schedule.UserId, NoTimeChanges, ct);

		var messages = await context.Messages
			.AsNoTracking()
			.Where(x => messageIds.Contains(x.Id))
			.OrderBy(x => x.Id)
			.ToListAsync();

		messages.Count.ShouldBe(messageIds.Count);
		messages.ShouldAllBe(m => m.IsVerified);
		messages.Select(m => m.Id).ShouldBe(messageIds.OrderBy(id => id).ToList());
	}

	[Fact]
	public async Task ApproveMessage_WhenSubsetOfIdsProvided_ShouldNotAffectOtherMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var first = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();
		var second = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();
		var untouched = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();

		await sut.ApproveMessage([first.Id, second.Id], schedule.UserId, NoTimeChanges, ct);

		var messages = await context.Messages
			.AsNoTracking()
			.Where(x => new[] { first.Id, second.Id, untouched.Id }.Contains(x.Id))
			.OrderBy(x => x.Id)
			.ToListAsync();

		messages.Where(x => x.Id == first.Id || x.Id == second.Id).ShouldAllBe(m => m.IsVerified);
		messages.Single(x => x.Id == untouched.Id).IsVerified.ShouldBeFalse();
	}

	[Fact]
	public async Task ApproveMessage_WithNonExistingIds_ShouldLeaveExistingMessagesUntouched()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var existing = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();

		await sut.ApproveMessage([Guid.NewGuid()], schedule.UserId, NoTimeChanges, ct);


		var actual = await context.Messages
			.AsNoTracking()
			.SingleAsync(x => x.Id == existing.Id, ct);

		actual.IsVerified.ShouldBeFalse();
	}

	[Fact]
	public async Task ApproveMessage_WithEmptyCollection_ShouldNotChangeVerificationStatus()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var verified = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(true).CreateAsync();
		var pending = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();


		var before = await context.Messages
			.AsNoTracking()
			.Where(x => x.ScheduleId == schedule.Id && (x.Id == verified.Id || x.Id == pending.Id))
			.OrderBy(x => x.Id)
			.Select(x => new { x.Id, x.IsVerified })
			.ToListAsync();

		await sut.ApproveMessage([], schedule.UserId, NoTimeChanges, ct);


		var after = await context.Messages
			.AsNoTracking()
			.Where(x => x.ScheduleId == schedule.Id && (x.Id == verified.Id || x.Id == pending.Id))
			.OrderBy(x => x.Id)
			.Select(x => new { x.Id, x.IsVerified })
			.ToListAsync();

		after.ShouldBe(before);
	}

	[Fact]
	public async Task ApproveMessage_WithNewTimePosting_ShouldMoveOnlyGivenMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var overdue = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithIsVerified(false)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(-1))
			.CreateAsync();
		var future = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync();
		var newTime = DateTimeOffset.UtcNow.AddHours(5);

		await sut.ApproveMessage(
			[overdue.Id, future.Id],
			schedule.UserId,
			new Dictionary<Guid, DateTimeOffset> { [overdue.Id] = newTime },
			ct);

		var messages = await context.Messages
			.AsNoTracking()
			.Where(x => x.Id == overdue.Id || x.Id == future.Id)
			.ToDictionaryAsync(x => x.Id, ct);
		messages[overdue.Id].IsVerified.ShouldBeTrue();
		messages[overdue.Id].TimePosting.ShouldBe(newTime, TimeSpan.FromMilliseconds(1));
		messages[future.Id].IsVerified.ShouldBeTrue();
		messages[future.Id].TimePosting.ShouldBe(future.TimePosting, TimeSpan.FromMilliseconds(1));
	}

	[Fact]
	public async Task ApproveMessage_WithAnotherUserMessage_ShouldNotChangeIt()
	{
		var ownerSchedule = await new ScheduleBuilder(context).CreateAsync();
		var strangerSchedule = await new ScheduleBuilder(context).CreateAsync();
		var foreign = await new MessageBuilder(context)
			.WithSchedule(ownerSchedule)
			.WithIsVerified(false)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(-1))
			.CreateAsync();

		await sut.ApproveMessage(
			[foreign.Id],
			strangerSchedule.UserId,
			new Dictionary<Guid, DateTimeOffset> { [foreign.Id] = DateTimeOffset.UtcNow.AddHours(5) },
			ct);

		var actual = await context.Messages
			.AsNoTracking()
			.SingleAsync(x => x.Id == foreign.Id, ct);
		actual.IsVerified.ShouldBeFalse();
		actual.TimePosting.ShouldBe(foreign.TimePosting, TimeSpan.FromMilliseconds(1));
	}
}
