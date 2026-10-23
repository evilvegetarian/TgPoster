using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class GetDashboardStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly GetDashboardStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetSchedulesAsync_ShouldReturnOnlySchedulesOfUser()
	{
		var mine = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();

		var result = await sut.GetSchedulesAsync(mine.UserId, DateTimeOffset.UtcNow, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([mine.Id]);
		result.Select(x => x.Id).ShouldNotContain(foreign.Id);
	}

	[Fact]
	public async Task GetSchedulesAsync_ShouldCountPlannedOnlyRegisterAndInHandleInFuture()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(1));
		var last = await CreateMessageAsync(schedule, MessageStatus.InHandle, now.AddHours(3));
		await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(-1));
		await CreateMessageAsync(schedule, MessageStatus.Send, now.AddHours(5));
		await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(6));
		await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(10), isVerified: false);

		var result = await sut.GetSchedulesAsync(schedule.UserId, now, CancellationToken.None);

		var row = result.Single();
		row.PlannedCount.ShouldBe(2);
		row.AwaitingApprovalCount.ShouldBe(1);
		row.LastPlannedAt.ShouldNotBeNull();
		row.LastPlannedAt.Value.ShouldBe(last.TimePosting, TimeSpan.FromMilliseconds(1));
		row.NextPostAt.ShouldNotBeNull();
		row.NextPostAt.Value.ShouldBe(now.AddHours(1), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task GetSchedulesAsync_ShouldCountFailedOnlyForLast24Hours()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(-1));
		await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(-30));
		await CreateMessageAsync(schedule, MessageStatus.Send, now.AddHours(-1));

		var result = await sut.GetSchedulesAsync(schedule.UserId, now, CancellationToken.None);

		result.Single().FailedLast24h.ShouldBe(1);
	}

	[Fact]
	public async Task GetSchedulesAsync_ShouldDetectSlots()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var withSlots = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		await new DayBuilder(context).WithScheduleId(withSlots.Id).WithTimePostings([new TimeOnly(10, 0)]).CreateAsync();
		var emptySlots = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		await new DayBuilder(context).WithScheduleId(emptySlots.Id).WithTimePostings([]).CreateAsync();
		var withoutDays = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();

		var result = await sut.GetSchedulesAsync(user.Id, DateTimeOffset.UtcNow, CancellationToken.None);

		result.Single(x => x.Id == withSlots.Id).HasSlots.ShouldBeTrue();
		result.Single(x => x.Id == emptySlots.Id).HasSlots.ShouldBeFalse();
		result.Single(x => x.Id == withoutDays.Id).HasSlots.ShouldBeFalse();
	}

	[Fact]
	public async Task GetUpcomingPostsAsync_ShouldReturnNearestFuturePostsOfUserOnly()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();
		var first = await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(1));
		var second = await CreateMessageAsync(schedule, MessageStatus.InHandle, now.AddHours(2), isVerified: false);
		await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(3));
		await CreateMessageAsync(schedule, MessageStatus.Register, now.AddHours(-1));
		await CreateMessageAsync(schedule, MessageStatus.Send, now.AddHours(4));
		await CreateMessageAsync(foreign, MessageStatus.Register, now.AddMinutes(30));

		var result = await sut.GetUpcomingPostsAsync(schedule.UserId, now, 2, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([first.Id, second.Id]);
		result[0].NeedApprove.ShouldBeFalse();
		result[1].NeedApprove.ShouldBeTrue();
		result[0].ScheduleName.ShouldBe(schedule.Name);
	}

	[Fact]
	public async Task GetUpcomingPostsAsync_ShouldCutTextAndCountOnlyTopLevelFiles()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		var message = new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(now.AddHours(1))
			.WithTextMessage(new string('a', 300))
			.WithPhotoMessageFile()
			.WithPhotoMessageFile()
			.WithVideoMessageFile()
			.Build();
		context.Messages.Add(message);
		await context.SaveChangesAsync();

		var result = await sut.GetUpcomingPostsAsync(schedule.UserId, now, 10, CancellationToken.None);

		var post = result.Single();
		post.TextPreview.ShouldBe(new string('a', 120));
		post.PhotoCount.ShouldBe(2);
		post.VideoCount.ShouldBe(1);
	}

	[Fact]
	public async Task GetUpcomingPostsAsync_ShouldReturnNullPreview_WhenNoText()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		var message = new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(now.AddHours(1))
			.Build();
		message.TextMessage = null;
		context.Messages.Add(message);
		await context.SaveChangesAsync();

		var result = await sut.GetUpcomingPostsAsync(schedule.UserId, now, 10, CancellationToken.None);

		result.Single().TextPreview.ShouldBeNull();
	}

	[Fact]
	public async Task GetFailedPostsAsync_ShouldReturnLatestFailedOfUserWithinWindow()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();
		var older = await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(-5), sendError: "boom");
		var newer = await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(-1));
		await CreateMessageAsync(schedule, MessageStatus.Error, now.AddHours(-30));
		await CreateMessageAsync(schedule, MessageStatus.Send, now.AddHours(-2));
		await CreateMessageAsync(foreign, MessageStatus.Error, now.AddHours(-1));

		var since = now.AddHours(-24);
		var result = await sut.GetFailedPostsAsync(schedule.UserId, since, 20, CancellationToken.None);
		var total = await sut.GetFailedPostsCountAsync(schedule.UserId, since, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([newer.Id, older.Id]);
		result[1].SendError.ShouldBe("boom");
		total.ShouldBe(2);
	}

	[Fact]
	public async Task GetRepostCountsAsync_ShouldCountOnlyLogsOfUser()
	{
		var schedule = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();
		await CreateRepostLogAsync(schedule, RepostStatus.Success);
		await CreateRepostLogAsync(schedule, RepostStatus.Success);
		await CreateRepostLogAsync(schedule, RepostStatus.Failed, "err");
		await CreateRepostLogAsync(schedule, RepostStatus.Pending);
		await CreateRepostLogAsync(foreign, RepostStatus.Success);
		await CreateRepostLogAsync(foreign, RepostStatus.Skipped);

		var result = await sut.GetRepostCountsAsync(
			schedule.UserId,
			DateTimeOffset.UtcNow.AddHours(-24),
			CancellationToken.None);

		result.ShouldBe(new DashboardRepostCounts(2, 1, 0));
	}

	[Fact]
	public async Task GetRepostTopErrorsAsync_ShouldGroupFailedErrorsOfUserOnly()
	{
		var schedule = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();
		await CreateRepostLogAsync(schedule, RepostStatus.Failed, "flood");
		await CreateRepostLogAsync(schedule, RepostStatus.Failed, "flood");
		await CreateRepostLogAsync(schedule, RepostStatus.Failed, "forbidden");
		await CreateRepostLogAsync(schedule, RepostStatus.Failed);
		await CreateRepostLogAsync(schedule, RepostStatus.Success, "ignored");
		await CreateRepostLogAsync(foreign, RepostStatus.Failed, "foreign");
		await CreateRepostLogAsync(foreign, RepostStatus.Failed, "foreign");
		await CreateRepostLogAsync(foreign, RepostStatus.Failed, "foreign");

		var result = await sut.GetRepostTopErrorsAsync(
			schedule.UserId,
			DateTimeOffset.UtcNow.AddHours(-24),
			3,
			CancellationToken.None);

		result.ShouldBe([new DashboardRepostError("flood", 2), new DashboardRepostError("forbidden", 1)]);
	}

	[Fact]
	public async Task GetSessionsAsync_ShouldReturnOnlySessionsOfUser()
	{
		var mine = await new TelegramSessionBuilder(context).WithStatus(TelegramSessionStatus.Authorized).CreateAsync();
		var foreign = await new TelegramSessionBuilder(context).CreateAsync();

		var result = await sut.GetSessionsAsync(mine.UserId, CancellationToken.None);

		var row = result.Single();
		row.Id.ShouldBe(mine.Id);
		row.Status.ShouldBe(TgPoster.Telegram.Models.TelegramSessionStatus.Authorized);
		result.Select(x => x.Id).ShouldNotContain(foreign.Id);
	}

	[Fact]
	public async Task GetParsingProblemsAsync_ShouldReturnOnlyFailedSourcesOfUser()
	{
		var schedule = await CreateScheduleWithSlotsAsync();
		var foreign = await CreateScheduleWithSlotsAsync();
		var failed = await new ChannelParsingSettingBuilder(context)
			.WithSchedule(schedule).WithStatus(ParsingStatus.Failed).CreateAsync();
		await new ChannelParsingSettingBuilder(context)
			.WithSchedule(schedule).WithStatus(ParsingStatus.Waiting).CreateAsync();
		await new ChannelParsingSettingBuilder(context)
			.WithSchedule(foreign).WithStatus(ParsingStatus.Failed).CreateAsync();

		var result = await sut.GetParsingProblemsAsync(schedule.UserId, CancellationToken.None);

		var row = result.Single();
		row.Id.ShouldBe(failed.Id);
		row.ScheduleName.ShouldBe(schedule.Name);
	}

	[Fact]
	public async Task GetWorkersAsync_ShouldReturnAllJobStates()
	{
		var jobName = $"dashboard-{Guid.NewGuid()}";
		context.WorkerJobStates.Add(new WorkerJobState
		{
			Id = Guid.NewGuid(),
			JobName = jobName,
			Status = WorkerJobStatus.Failed,
			LastError = "boom"
		});
		await context.SaveChangesAsync();

		var result = await sut.GetWorkersAsync(CancellationToken.None);

		var worker = result.Single(x => x.JobName == jobName);
		worker.Status.ShouldBe(TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus.WorkerJobStateStatus.Failed);
		worker.LastError.ShouldBe("boom");
	}

	private async Task<Schedule> CreateScheduleWithSlotsAsync()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		await new DayBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTimePostings([new TimeOnly(10, 0)])
			.CreateAsync();
		return schedule;
	}

	private Task<Message> CreateMessageAsync(
		Schedule schedule,
		MessageStatus status,
		DateTimeOffset timePosting,
		bool isVerified = true,
		string? sendError = null
	) =>
		new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(status)
			.WithTimeMessage(timePosting)
			.WithIsVerified(isVerified)
			.WithSendError(sendError)
			.CreateAsync();

	private async Task CreateRepostLogAsync(Schedule schedule, RepostStatus status, string? error = null)
	{
		var settings = await new RepostSettingsBuilder(context).WithSchedule(schedule).CreateAsync();
		var destination = await new RepostDestinationBuilder(context).WithRepostSettings(settings).CreateAsync();
		var message = new MessageBuilder(context).WithScheduleId(schedule.Id).Create();
		context.Add(new RepostLog
		{
			Id = Guid.NewGuid(),
			MessageId = message.Id,
			RepostDestinationId = destination.Id,
			Status = status,
			Error = error
		});
		await context.SaveChangesAsync();
	}
}
