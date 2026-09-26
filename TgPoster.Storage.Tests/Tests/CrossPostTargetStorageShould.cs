using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.CrossPosting;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class CrossPostTargetStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CrossPostTargetStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	private static CreateCrossPostTargetCommand CreateCommand(Guid scheduleId, Guid accountId) =>
		new(scheduleId, accountId, CrossPostFormat.Teaser, CrossPostLinkTarget.Post, null, null, true, false, 0);

	[Fact]
	public async Task CreateAsync_ShouldCreateActiveTarget()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();

		var id = await sut.CreateAsync(CreateCommand(schedule.Id, account.Id), CancellationToken.None);

		var target = await context.CrossPostTargets
			.AsNoTracking()
			.FirstAsync(x => x.Id == id);

		target.ScheduleId.ShouldBe(schedule.Id);
		target.SocialAccountId.ShouldBe(account.Id);
		target.IsActive.ShouldBeTrue();
		target.Format.ShouldBe(CrossPostFormat.Teaser);
		target.LinkTarget.ShouldBe(CrossPostLinkTarget.Post);
	}

	[Fact]
	public async Task ExistsAsync_ShouldCheckOwnershipOfSchedule()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var id = await sut.CreateAsync(CreateCommand(schedule.Id, account.Id), CancellationToken.None);

		var scheduleExists = await sut.ScheduleExistsAsync(schedule.Id, user.Id, CancellationToken.None);
		var targetExists = await sut.ExistsAsync(id, schedule.Id, CancellationToken.None);
		var otherSchedule = await sut.ExistsAsync(id, Guid.NewGuid(), CancellationToken.None);

		scheduleExists.ShouldBeTrue();
		targetExists.ShouldBeTrue();
		otherSchedule.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateAsync_ShouldChangeFields()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var id = await sut.CreateAsync(CreateCommand(schedule.Id, account.Id), CancellationToken.None);

		var command = new UpdateCrossPostTargetCommand(
			schedule.Id, id, false, CrossPostFormat.Full, CrossPostLinkTarget.Channel,
			null, "Читайте", false, true, 45);

		await sut.UpdateAsync(command, CancellationToken.None);

		var target = await context.CrossPostTargets
			.AsNoTracking()
			.FirstAsync(x => x.Id == id);

		target.IsActive.ShouldBeFalse();
		target.Format.ShouldBe(CrossPostFormat.Full);
		target.LinkTarget.ShouldBe(CrossPostLinkTarget.Channel);
		target.CallToAction.ShouldBe("Читайте");
		target.IncludeMedia.ShouldBeFalse();
		target.IncludeParsed.ShouldBeTrue();
		target.DelayMinutes.ShouldBe(45);
	}

	[Fact]
	public async Task DeleteAsync_ShouldSoftDeleteTarget()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var id = await sut.CreateAsync(CreateCommand(schedule.Id, account.Id), CancellationToken.None);

		await sut.DeleteAsync(id, CancellationToken.None);

		var deleted = await context.CrossPostTargets
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == id);

		deleted.ShouldNotBeNull();
		deleted.Deleted.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetContextAsync_ShouldReturnLastMessageText()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		await sut.CreateAsync(CreateCommand(schedule.Id, account.Id), CancellationToken.None);

		new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithTextMessage("Старый текст")
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(-2))
			.Create();
		new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithTextMessage("Новый текст")
			.WithTimeMessage(DateTimeOffset.UtcNow)
			.Create();

		var result = await sut.GetContextAsync(schedule.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.LastMessageText.ShouldBe("Новый текст");
		result.Targets.Count.ShouldBe(1);
	}
}
