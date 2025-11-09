using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ApproveMessagesStorageShould : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;

	private readonly PosterContext context;
	private readonly ApproveMessagesStorage sut;

	public ApproveMessagesStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new ApproveMessagesStorage(context);
	}

	[Fact]
	public async Task ApproveMessage_WithExistingUnverifiedMessages_ShouldSetIsVerifiedToTrue()
	{
		var messageIds = new List<Message>
		{
			await new MessageBuilder(context).WithIsVerified(false).CreateAsync(Ct),
			await new MessageBuilder(context).WithIsVerified(false).CreateAsync(Ct),
			await new MessageBuilder(context).WithIsVerified(false).CreateAsync(Ct),
		}.Select(x => x.Id).ToList();

		await sut.ApproveMessage(messageIds, Ct);
		context.ChangeTracker.Clear();

		var messages = await context.Messages
			.AsNoTracking()
			.Where(x => messageIds.Contains(x.Id))
			.OrderBy(x => x.Id)
			.ToListAsync(Ct);

		messages.Count.ShouldBe(messageIds.Count);
		messages.ShouldAllBe(m => m.IsVerified);
		messages.Select(m => m.Id).ShouldBe(messageIds.OrderBy(id => id).ToList());
	}

	[Fact]
	public async Task ApproveMessage_WhenSubsetOfIdsProvided_ShouldNotAffectOtherMessages()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var first = new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).Create();
		var second = new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).Create();
		var untouched = new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).Create();

		await sut.ApproveMessage([first.Id, second.Id], Ct);
		context.ChangeTracker.Clear();

		var messages = await context.Messages
			.AsNoTracking()
			.Where(x => new[] { first.Id, second.Id, untouched.Id }.Contains(x.Id))
			.OrderBy(x => x.Id)
			.ToListAsync(Ct);

		messages.Where(x => x.Id == first.Id || x.Id == second.Id).ShouldAllBe(m => m.IsVerified);
		messages.Single(x => x.Id == untouched.Id).IsVerified.ShouldBeFalse();
	}

	[Fact]
	public async Task ApproveMessage_WithNonExistingIds_ShouldLeaveExistingMessagesUntouched()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(Ct);
		var existing = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(Ct);

		await sut.ApproveMessage([Guid.NewGuid()], Ct);

		context.ChangeTracker.Clear();

		var actual = await context.Messages
			.AsNoTracking()
			.SingleAsync(x => x.Id == existing.Id, Ct);

		actual.IsVerified.ShouldBeFalse();
	}

	[Fact]
	public async Task ApproveMessage_WithEmptyCollection_ShouldNotChangeVerificationStatus()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(Ct);
		var verified = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(true).CreateAsync(Ct);
		var pending = await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(Ct);

		context.ChangeTracker.Clear();

		var before = await context.Messages
			.AsNoTracking()
			.Where(x => x.ScheduleId == schedule.Id && (x.Id == verified.Id || x.Id == pending.Id))
			.OrderBy(x => x.Id)
			.Select(x => new { x.Id, x.IsVerified })
			.ToListAsync(Ct);

		await sut.ApproveMessage([], Ct);

		context.ChangeTracker.Clear();

		var after = await context.Messages
			.AsNoTracking()
			.Where(x => x.ScheduleId == schedule.Id && (x.Id == verified.Id || x.Id == pending.Id))
			.OrderBy(x => x.Id)
			.Select(x => new { x.Id, x.IsVerified })
			.ToListAsync(Ct);

		after.ShouldBe(before);
	}
}