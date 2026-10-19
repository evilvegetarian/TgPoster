using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class SetRepostDestinationsActiveStorage(PosterContext context, GuidFactory guidFactory)
	: ISetRepostDestinationsActiveStorage
{
	public Task<Guid?> GetTelegramSessionIdAsync(Guid settingsId, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Id == settingsId && x.Schedule.UserId == userId)
			.Select(x => (Guid?)x.TelegramSessionId)
			.FirstOrDefaultAsync(ct);
	}

	public Task<List<RepostDestinationState>> GetDestinationsAsync(
		Guid settingsId,
		List<Guid> destinationIds,
		CancellationToken ct
	)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.RepostSettingsId == settingsId && destinationIds.Contains(x.Id))
			.Select(x => new RepostDestinationState(x.Id, x.ChatId, x.IsActive, x.ChatStatus))
			.ToListAsync(ct);
	}

	public Task<List<long>> GetBannedChatIdsAsync(Guid telegramSessionId, List<long> chatIds, CancellationToken ct)
	{
		return context.TelegramSessionChatBans
			.Where(x => x.TelegramSessionId == telegramSessionId && chatIds.Contains(x.ChatId))
			.Select(x => x.ChatId)
			.ToListAsync(ct);
	}

	public async Task AddAccountBansAsync(
		Guid telegramSessionId,
		IReadOnlyDictionary<long, string> errorsByChatId,
		CancellationToken ct
	)
	{
		foreach (var (chatId, error) in errorsByChatId)
		{
			await context.AddOrUpdateChatBanAsync(guidFactory, telegramSessionId, chatId, error, ct);
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task ActivateAsync(List<Guid> destinationIds, CancellationToken ct)
	{
		var destinations = await context.Set<RepostDestination>()
			.Where(x => destinationIds.Contains(x.Id))
			.ToListAsync(ct);

		foreach (var destination in destinations)
		{
			destination.IsActive = true;
			destination.ChatStatus = ChatStatus.Active;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task DeactivateAsync(List<Guid> destinationIds, CancellationToken ct)
	{
		var destinations = await context.Set<RepostDestination>()
			.Where(x => destinationIds.Contains(x.Id))
			.ToListAsync(ct);

		foreach (var destination in destinations)
		{
			destination.IsActive = false;
		}

		await context.SaveChangesAsync(ct);
	}
}
