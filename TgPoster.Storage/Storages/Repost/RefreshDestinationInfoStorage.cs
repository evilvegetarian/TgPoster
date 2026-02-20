using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RefreshDestinationInfoStorage(PosterContext context) : IRefreshDestinationInfoStorage
{
	public Task<Guid?> GetTelegramSessionIdAsync(Guid destinationId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.Id == destinationId)
			.Select(x => (Guid?)x.RepostSettings.TelegramSessionId)
			.FirstOrDefaultAsync(ct);
	}

	public Task<long?> GetChatIdAsync(Guid destinationId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.Id == destinationId)
			.Select(x => (long?)x.ChatId)
			.FirstOrDefaultAsync(ct);
	}

	public async Task UpdateDestinationInfoAsync(
		Guid destinationId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		CancellationToken ct)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == destinationId, ct);

		destination.Title = title;
		destination.Username = username;
		destination.MemberCount = memberCount;
		destination.ChatType = chatType;
		destination.ChatStatus = chatStatus;
		destination.AvatarBase64 = avatarBase64;
		destination.InfoUpdatedAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}
}
