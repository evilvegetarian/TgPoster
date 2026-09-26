using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;
using TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;
using TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.CrossPosting;

/// <summary>
///     Хранилище для работы со связками расписаний и аккаунтов соцсетей
/// </summary>
internal sealed class CrossPostTargetStorage(PosterContext context, GuidFactory guidFactory)
	: IListCrossPostTargetsStorage,
		ICreateCrossPostTargetStorage,
		IUpdateCrossPostTargetStorage,
		IDeleteCrossPostTargetStorage,
		IPreviewCrossPostStorage
{
	public Task<bool> ScheduleExistsAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public Task<List<CrossPostTargetResponse>> GetAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.CrossPostTargets
			.AsNoTracking()
			.Where(x => x.ScheduleId == scheduleId)
			.OrderBy(x => x.Created)
			.Select(x => new CrossPostTargetResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				SocialAccountId = x.SocialAccountId,
				Platform = x.SocialAccount.Platform,
				AccountName = x.SocialAccount.Name,
				AccountStatus = x.SocialAccount.Status,
				IsActive = x.IsActive,
				Format = x.Format,
				LinkTarget = x.LinkTarget,
				CustomLink = x.CustomLink,
				CallToAction = x.CallToAction,
				IncludeMedia = x.IncludeMedia,
				IncludeParsed = x.IncludeParsed,
				DelayMinutes = x.DelayMinutes
			})
			.ToListAsync(ct);
	}

	public Task<bool> SocialAccountExistsAsync(Guid accountId, Guid userId, CancellationToken ct)
	{
		return context.SocialAccounts.AnyAsync(x => x.Id == accountId && x.UserId == userId, ct);
	}

	public Task<bool> TargetExistsAsync(Guid scheduleId, Guid accountId, CancellationToken ct)
	{
		return context.CrossPostTargets.AnyAsync(
			x => x.ScheduleId == scheduleId && x.SocialAccountId == accountId,
			ct);
	}

	public async Task<Guid> CreateAsync(CreateCrossPostTargetCommand command, CancellationToken ct)
	{
		var target = new CrossPostTarget
		{
			Id = guidFactory.New(),
			ScheduleId = command.ScheduleId,
			SocialAccountId = command.SocialAccountId,
			IsActive = true,
			Format = command.Format,
			LinkTarget = command.LinkTarget,
			CustomLink = command.CustomLink,
			CallToAction = command.CallToAction,
			IncludeMedia = command.IncludeMedia,
			IncludeParsed = command.IncludeParsed,
			DelayMinutes = command.DelayMinutes
		};

		await context.CrossPostTargets.AddAsync(target, ct);
		await context.SaveChangesAsync(ct);

		return target.Id;
	}

	public Task<bool> ExistsAsync(Guid id, Guid scheduleId, CancellationToken ct)
	{
		return context.CrossPostTargets.AnyAsync(x => x.Id == id && x.ScheduleId == scheduleId, ct);
	}

	public async Task UpdateAsync(UpdateCrossPostTargetCommand command, CancellationToken ct)
	{
		var target = await context.CrossPostTargets
			.FirstAsync(x => x.Id == command.Id, ct);

		target.IsActive = command.IsActive;
		target.Format = command.Format;
		target.LinkTarget = command.LinkTarget;
		target.CustomLink = command.CustomLink;
		target.CallToAction = command.CallToAction;
		target.IncludeMedia = command.IncludeMedia;
		target.IncludeParsed = command.IncludeParsed;
		target.DelayMinutes = command.DelayMinutes;

		await context.SaveChangesAsync(ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct)
	{
		var target = await context.CrossPostTargets
			.FirstAsync(x => x.Id == id, ct);

		context.Remove(target);
		await context.SaveChangesAsync(ct);
	}

	public async Task<CrossPostPreviewContext?> GetContextAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		var schedule = await context.Schedules
			.AsNoTracking()
			.Where(x => x.Id == scheduleId && x.UserId == userId)
			.Select(x => new { x.ChannelName })
			.FirstOrDefaultAsync(ct);

		if (schedule is null)
		{
			return null;
		}

		var lastMessageText = await context.Messages
			.AsNoTracking()
			.Where(x => x.ScheduleId == scheduleId && x.TextMessage != null && x.TextMessage != "")
			.OrderByDescending(x => x.TimePosting)
			.Select(x => x.TextMessage)
			.FirstOrDefaultAsync(ct);

		var targets = await context.CrossPostTargets
			.AsNoTracking()
			.Where(x => x.ScheduleId == scheduleId)
			.OrderBy(x => x.Created)
			.Select(x => new CrossPostPreviewTargetDto
			{
				SocialAccountId = x.SocialAccountId,
				Platform = x.SocialAccount.Platform,
				AccountName = x.SocialAccount.Name,
				Format = x.Format,
				LinkTarget = x.LinkTarget,
				CustomLink = x.CustomLink,
				CallToAction = x.CallToAction
			})
			.ToListAsync(ct);

		return new CrossPostPreviewContext
		{
			ChannelName = schedule.ChannelName,
			LastMessageText = lastMessageText,
			Targets = targets
		};
	}

	public Task<CrossPostPreviewTargetDto?> GetAccountAsync(Guid accountId, Guid userId, CancellationToken ct)
	{
		return context.SocialAccounts
			.AsNoTracking()
			.Where(x => x.Id == accountId && x.UserId == userId)
			.Select(x => new CrossPostPreviewTargetDto
			{
				SocialAccountId = x.Id,
				Platform = x.Platform,
				AccountName = x.Name,
				Format = CrossPostFormat.Teaser,
				LinkTarget = CrossPostLinkTarget.Post
			})
			.FirstOrDefaultAsync(ct);
	}
}
