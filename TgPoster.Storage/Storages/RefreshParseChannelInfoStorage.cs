using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class RefreshParseChannelInfoStorage(PosterContext context) : IRefreshParseChannelInfoStorage
{
	public async Task<(Guid TelegramSessionId, string Channel)?> GetParseChannelInfoAsync(Guid id, CancellationToken ct)
	{
		var result = await context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.Select(x => new { x.TelegramSessionId, x.Channel })
			.FirstOrDefaultAsync(ct);

		if (result is null)
			return null;

		return (result.TelegramSessionId, result.Channel);
	}

	public async Task UpdateTotalMessagesCountAsync(Guid id, int? totalMessagesCount, CancellationToken ct)
	{
		var entity = await context.ChannelParsingParameters.FirstAsync(x => x.Id == id, ct);
		entity.TotalMessagesCount = totalMessagesCount;
		await context.SaveChangesAsync(ct);
	}
}
