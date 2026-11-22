using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Discover.SimilarChannels;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class SimilarDiscoverStorage(PosterContext context, GuidFactory guidFactory) : ISimilarDiscoverStorage
{
	public Task<SimilarDiscoverSeed?> GetSeedAsync(Guid id, CancellationToken ct) =>
		context.DiscoveredChannels.Where(c => c.Id == id)
			.Select(c => new SimilarDiscoverSeed(c.Id, c.TelegramId, c.Username, c.PeerType,
				c.Category, c.Subcategory, c.Language, c.Tags))
			.FirstOrDefaultAsync(ct);

	public Task<Guid?> GetSessionAsync(Guid userId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(s => s.UserId == userId && s.IsActive && s.Status == TelegramSessionStatus.Authorized
				&& s.Purposes.Contains(TelegramSessionPurpose.Discover)
				&& (s.FloodWaitUntil == null || s.FloodWaitUntil <= DateTimeOffset.UtcNow))
			.OrderBy(s => s.Id)
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);

	public Task<List<SimilarDiscoverChannel>> FindLocalAsync(SimilarDiscoverSeed seed, CancellationToken ct)
	{
		var tags = (seed.Tags ?? []).Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToArray();
		var subcategory = seed.Subcategory?.Trim().ToLowerInvariant();
		var category = seed.Category?.Trim().ToLowerInvariant();
		var language = seed.Language?.Trim().ToLowerInvariant();
		var username = seed.Username?.ToLowerInvariant();
		if (string.IsNullOrEmpty(subcategory) && string.IsNullOrEmpty(category) && tags.Length == 0)
			return Task.FromResult<List<SimilarDiscoverChannel>>([]);

		return context.DiscoveredChannels
			.Where(c => c.Id != seed.Id && (seed.TelegramId == null || c.TelegramId != seed.TelegramId)
				&& (username == null || c.Username == null || c.Username.ToLower() != username))
			.Select(c => new
			{
				Channel = c,
				Subcategory = !string.IsNullOrEmpty(subcategory) && c.Subcategory != null && c.Subcategory.Trim().ToLower() == subcategory,
				Category = !string.IsNullOrEmpty(category) && c.Category != null && c.Category.Trim().ToLower() == category,
				Language = !string.IsNullOrEmpty(language) && c.Language != null && c.Language.Trim().ToLower() == language,
				Tags = c.Tags == null ? 0 : c.Tags.Count(t => tags.Contains(t.Trim().ToLower()))
			})
			.Where(x => x.Subcategory || x.Tags > 0 || x.Category)
			.OrderByDescending(x => x.Subcategory).ThenByDescending(x => x.Tags)
			.ThenByDescending(x => x.Category).ThenByDescending(x => x.Language)
			.ThenByDescending(x => x.Channel.ParticipantsCount ?? 0).ThenBy(x => x.Channel.Id)
			.Take(20)
			.Select(x => new SimilarDiscoverChannel(x.Channel.Id, x.Channel.TelegramId, x.Channel.Username,
				x.Channel.Title, x.Channel.TgUrl, x.Channel.ParticipantsCount))
			.ToListAsync(ct);
	}

	public Task<List<KnownDiscoverPeer>> FindKnownAsync(long[] telegramIds, string[] usernames, CancellationToken ct)
	{
		var normalized = usernames.Select(u => u.ToLowerInvariant()).ToArray();
		return context.DiscoveredChannels.IgnoreQueryFilters()
			.Where(c => (c.TelegramId.HasValue && telegramIds.Contains(c.TelegramId.Value))
				|| (c.Username != null && normalized.Contains(c.Username.ToLower())))
			.Select(c => new KnownDiscoverPeer(c.Id, c.TelegramId, c.Username, c.IsBanned || c.Deleted != null))
			.ToListAsync(ct);
	}

	public Task<int> AddAsync(Guid sourceId, Guid userId, IReadOnlyList<SimilarDiscoverChannel> channels, CancellationToken ct)
	{
		var ids = channels.Select(_ => guidFactory.New()).ToArray();
		var telegramIds = channels.Select(c => c.TelegramId!.Value).ToArray();
		var usernames = channels.Select(c => c.Username!).ToArray();
		var titles = channels.Select(c => c.Title).ToArray();
		var counts = channels.Select(c => c.ParticipantsCount).ToArray();

		return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
		{
			await using var transaction = await context.Database.BeginTransactionAsync(ct);
			// TelegramId допускает дубли в старом каталоге. Два одновременных выбора должны увидеть записи друг друга
			await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('discover-similar'), 0)", ct);
			var added = await context.Database.ExecuteSqlInterpolatedAsync($"""
				INSERT INTO "DiscoveredChannels"
					("Id", "TelegramId", "Username", "Title", "ParticipantsCount", "TgUrl", "PeerType",
					 "Status", "DiscoveredFromChannelId", "Created", "CreatedById")
				SELECT p.id, p.tg_id, p.username, p.title, p.members, 'https://t.me/' || p.username, 'channel',
				       {(int)DiscoveryStatus.Pending}, {sourceId}, now(), {userId}
				FROM unnest({ids}, {telegramIds}, {usernames}, {titles}, {counts}) AS p(id, tg_id, username, title, members)
				WHERE EXISTS (SELECT 1 FROM "DiscoveredChannels" s
				              WHERE s."Id" = {sourceId} AND NOT s."IsBanned" AND s."Deleted" IS NULL)
				  AND NOT EXISTS (SELECT 1 FROM "DiscoveredChannels" c
				                  WHERE c."TelegramId" = p.tg_id OR lower(c."Username") = lower(p.username))
				ON CONFLICT DO NOTHING
				""", ct);
			await transaction.CommitAsync(ct);
			return added;
		});
	}
}
