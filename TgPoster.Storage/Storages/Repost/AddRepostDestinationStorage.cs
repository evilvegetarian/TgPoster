using Microsoft.EntityFrameworkCore;
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
		CancellationToken ct)
	{
		var destination = new RepostDestination
		{
			Id = guidFactory.New(),
			RepostSettingsId = repostSettingsId,
			ChatId = chatId,
			IsActive = true
		};

		await context.AddAsync(destination, ct);
		await context.SaveChangesAsync(ct);

		return destination.Id;
	}
}
