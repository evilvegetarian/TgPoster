using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;
using TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;
using TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.SocialAccounts;

/// <summary>
///     Хранилище для работы с аккаунтами соцсетей
/// </summary>
internal sealed class SocialAccountStorage(PosterContext context, GuidFactory guidFactory)
	: IListSocialAccountsStorage, IDeleteSocialAccountStorage, IConnectBlueskyStorage
{
	public Task<List<SocialAccountResponse>> GetAsync(Guid userId, CancellationToken ct)
	{
		return context.SocialAccounts
			.AsNoTracking()
			.Where(x => x.UserId == userId)
			.OrderBy(x => x.Created)
			.Select(x => new SocialAccountResponse
			{
				Id = x.Id,
				Platform = x.Platform,
				Name = x.Name,
				Status = x.Status,
				TokenExpiresAt = x.TokenExpiresAt,
				LastError = x.LastError,
				Created = x.Created!.Value
			})
			.ToListAsync(ct);
	}

	public Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.SocialAccounts.AnyAsync(x => x.Id == id && x.UserId == userId, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct)
	{
		var account = await context.SocialAccounts
			.Include(x => x.CrossPostTargets)
			.FirstAsync(x => x.Id == id, ct);

		foreach (var target in account.CrossPostTargets)
		{
			context.CrossPostTargets.Remove(target);
		}

		context.SocialAccounts.Remove(account);
		await context.SaveChangesAsync(ct);
	}

	public async Task<Guid> UpsertAsync(
		Guid userId,
		SocialPlatform platform,
		string name,
		string externalUserId,
		string encryptedSecret,
		DateTimeOffset? tokenExpiresAt,
		CancellationToken ct)
	{
		var account = await context.SocialAccounts
			.FirstOrDefaultAsync(
				x => x.UserId == userId && x.Platform == platform && x.ExternalUserId == externalUserId,
				ct);

		if (account is not null)
		{
			account.Name = name;
			account.Secret = encryptedSecret;
			account.TokenExpiresAt = tokenExpiresAt;
			account.Status = SocialAccountStatus.Active;
			account.LastError = null;
		}
		else
		{
			account = new SocialAccount
			{
				Id = guidFactory.New(),
				UserId = userId,
				Platform = platform,
				Name = name,
				ExternalUserId = externalUserId,
				Secret = encryptedSecret,
				TokenExpiresAt = tokenExpiresAt,
				Status = SocialAccountStatus.Active
			};
			await context.SocialAccounts.AddAsync(account, ct);
		}

		await context.SaveChangesAsync(ct);
		return account.Id;
	}
}
