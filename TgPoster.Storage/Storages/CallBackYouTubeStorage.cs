using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class CallBackYouTubeStorage(PosterContext context) : ICallBackYouTubeStorage
{
	public Task<(string ClientId, string ClientSecret)> GetClients(
		Guid accountYouTubeId,
		Guid userId,
		CancellationToken ct
	)
	{
		return context.YouTubeAccounts
			.Where(x => x.Id == accountYouTubeId)
			//.Where(x => x.UserId == userId)
			.Select(x => new ValueTuple<string, string>(x.ClientId, x.ClientSecret))
			.FirstOrDefaultAsync(ct);
	}

	public async Task AddToken(Guid accountYouTubeId, string accessToken, string? refreshToken, CancellationToken ct)
	{
		var entity = await context.YouTubeAccounts.FirstOrDefaultAsync(x => x.Id == accountYouTubeId, ct);
		entity!.AccessToken = accessToken;
		entity.RefreshToken = refreshToken;
		await context.SaveChangesAsync(ct);
	}

	public async Task AddToken(
		Guid accountYouTubeId,
		string accessToken,
		string refreshToken,
		string channelName,
		string? channelId,
		CancellationToken ct
	)
	{
		var entity = await context.YouTubeAccounts.FirstOrDefaultAsync(x => x.Id == accountYouTubeId, ct);
		entity!.AccessToken = accessToken;
		entity.RefreshToken = refreshToken;
		entity.Name = channelName;
		await context.SaveChangesAsync(ct);
	}
}