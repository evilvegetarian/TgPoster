using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class AddRepostDestinationStorage(PosterContext context, GuidFactory guidFactory) : IAddRepostDestinationStorage
{
	public Task<Guid?> GetTelegramSessionIdAsync(Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Id == repostSettingsId)
			.Select(x => (Guid?)x.TelegramSessionId)
			.FirstOrDefaultAsync(ct);
	}

	public Task<bool> DestinationExistsAsync(Guid repostSettingsId, long chatIdentifier, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.AnyAsync(x => x.RepostSettingsId == repostSettingsId && x.ChatId == chatIdentifier, ct);
	}

	public async Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		CancellationToken ct)
	{
		var destination = new RepostDestination
		{
			Id = guidFactory.New(),
			RepostSettingsId = repostSettingsId,
			ChatId = chatId,
			IsActive = true,
			Title = title,
			Username = username,
			MemberCount = memberCount,
			ChatType = chatType,
			ChatStatus = chatStatus,
			AvatarBase64 = avatarBase64,
			InfoUpdatedAt = DateTimeOffset.UtcNow
		};

		await context.AddAsync(destination, ct);
		await context.SaveChangesAsync(ct);

		return destination.Id;
	}
}
