using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ScrapeChannel;

namespace TgPoster.Storage.Storages.TgStat;

internal sealed class ScrapeChannelStorage(PosterContext context, GuidFactory guidFactory) : IScrapeChannelStorage
{
	public async Task UpsertChannelAsync(
		string username,
		string? title,
		string? description,
		string? avatarUrl,
		int? participantsCount,
		string? peerType,
		string? tgUrl,
		CancellationToken ct)
	{
		var existing = await context.DiscoveredChannels
			.FirstOrDefaultAsync(x => x.Username == username, ct);

		if (existing is not null)
		{
			existing.Title = title;
			existing.Description = description;
			existing.AvatarUrl = avatarUrl;
			existing.ParticipantsCount = participantsCount;
			existing.PeerType = peerType;
			existing.TgUrl = tgUrl;
		}
		else
		{
			context.DiscoveredChannels.Add(new DiscoveredChannel
			{
				Id = guidFactory.New(),
				Username = username,
				Title = title,
				Description = description,
				AvatarUrl = avatarUrl,
				ParticipantsCount = participantsCount,
				PeerType = peerType,
				TgUrl = tgUrl,
				Status = DiscoveryStatus.Pending
			});
		}

		await context.SaveChangesAsync(ct);
	}
}
