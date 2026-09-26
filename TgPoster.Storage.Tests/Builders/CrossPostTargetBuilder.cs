using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class CrossPostTargetBuilder(PosterContext context)
{
	private readonly CrossPostTarget target = new()
	{
		Id = Guid.NewGuid(),
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		SocialAccountId = new SocialAccountBuilder(context).Create().Id,
		Format = CrossPostFormat.Teaser,
		LinkTarget = CrossPostLinkTarget.Post,
		DelayMinutes = 0
	};

	public CrossPostTargetBuilder WithSchedule(Schedule schedule)
	{
		target.Schedule = schedule;
		target.ScheduleId = schedule.Id;
		return this;
	}

	public CrossPostTargetBuilder WithScheduleId(Guid scheduleId)
	{
		target.ScheduleId = scheduleId;
		return this;
	}

	public CrossPostTargetBuilder WithSocialAccount(SocialAccount account)
	{
		target.SocialAccount = account;
		target.SocialAccountId = account.Id;
		return this;
	}

	public CrossPostTargetBuilder WithIsActive(bool isActive)
	{
		target.IsActive = isActive;
		return this;
	}

	public CrossPostTargetBuilder WithFormat(CrossPostFormat format)
	{
		target.Format = format;
		return this;
	}

	public CrossPostTargetBuilder WithDelayMinutes(int minutes)
	{
		target.DelayMinutes = minutes;
		return this;
	}

	public CrossPostTargetBuilder WithIncludeParsed(bool includeParsed)
	{
		target.IncludeParsed = includeParsed;
		return this;
	}

	public CrossPostTarget Build() => target;

	public CrossPostTarget Create()
	{
		context.CrossPostTargets.AddRange(target);
		context.SaveChanges();
		return target;
	}

	public async Task<CrossPostTarget> CreateAsync(CancellationToken ct = default)
	{
		await context.CrossPostTargets.AddRangeAsync(target);
		await context.SaveChangesAsync(ct);
		return target;
	}
}
