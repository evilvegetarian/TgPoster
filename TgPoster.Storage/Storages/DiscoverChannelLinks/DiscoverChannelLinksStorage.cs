using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

namespace TgPoster.Storage.Storages.DiscoverChannelLinks;

internal sealed class DiscoverChannelLinksStorage(PosterContext context, GuidFactory guidFactory)
	: IDiscoverChannelLinksStorage
{
	public Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(int channelBatchSize, CancellationToken ct)
	{
		//Пока это только ищу
		var query = context.DiscoveredChannels
			.Where(x => x.PeerType == "chat")
			.Where(x => x.Category == "18+");

		return query
			.Where(x => x.Status == DiscoveryStatus.Pending || x.Status == DiscoveryStatus.Completed)
			.Where(x => x.Username != null)
			.OrderBy(x => x.LastDiscoveredAt != null)
			.Take(channelBatchSize)
			.Select(x => new DiscoverChannelDto
			{
				Id = x.Id,
				Username = x.Username,
				TelegramId = x.TelegramId,
				InviteHash = x.InviteHash,
				LastParsedId = x.LastParsedId
			})
			.ToListAsync(ct);
	}

	public async Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct)
	{
		var matches = await FindAllMatchingAsync(upsert.Username, upsert.TelegramId, upsert.InviteHash, ct);

		if (matches.Count > 0)
		{
			var primary = ConsolidateDuplicates(matches);
			ApplyUpsertFields(primary, upsert);
			await context.SaveChangesAsync(ct);
			return;
		}

		var entity = new DiscoveredChannel
		{
			Id = guidFactory.New(),
			Username = upsert.Username,
			TgUrl = upsert.TgUrl,
			LastParsedId = upsert.LastParsedId,
			TelegramId = upsert.TelegramId,
			PeerType = upsert.PeerType,
			Title = upsert.Title,
			Description = upsert.Description,
			AvatarUrl = upsert.AvatarUrl,
			ParticipantsCount = upsert.ParticipantsCount,
			InviteHash = upsert.InviteHash,
			DiscoveredFromChannelId = upsert.DiscoveredFromChannelId,
			Status = DiscoveryStatus.Pending
		};

		context.DiscoveredChannels.Add(entity);
		await context.SaveChangesAsync(ct);
	}

	public async Task ChannelBanned(Guid id, CancellationToken ct)
	{
		var entity = await context.DiscoveredChannels.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
		entity!.IsBanned = true;
		await context.SaveChangesAsync(ct);
	}

	public async Task MarkAsSkippedAsync(Guid id, CancellationToken ct)
	{
		var entity = await context.DiscoveredChannels.FirstAsync(x => x.Id == id, ct);
		entity.Status = DiscoveryStatus.Skipped;
		entity.LastDiscoveredAt = DateTimeOffset.UtcNow;
		await context.SaveChangesAsync(ct);
	}

	public async Task BulkUpsertAsync(IReadOnlyCollection<DiscoveredPeerUpsert> upserts, CancellationToken ct)
	{
		if (upserts.Count == 0)
			return;

		var usernames = upserts
			.Where(x => x.Username is not null)
			.Select(x => x.Username!)
			.Distinct()
			.ToArray();

		var telegramIds = upserts
			.Where(x => x.TelegramId is not null)
			.Select(x => x.TelegramId!.Value)
			.Distinct()
			.ToArray();

		var inviteHashes = upserts
			.Where(x => x.InviteHash is not null)
			.Select(x => x.InviteHash!)
			.Distinct()
			.ToArray();

		// IgnoreQueryFilters: уникальные индексы IX_DiscoveredChannels_Username и
		// IX_DiscoveredChannels_InviteHash покрывают и забаненные строки. Без этого
		// при наличии в БД забаненной строки с тем же Username/InviteHash мы бы её
		// не увидели и попытались вставить новую → нарушение unique constraint
		var existing = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.Where(x =>
				(x.Username != null && usernames.Contains(x.Username))
				|| (x.TelegramId != null && telegramIds.Contains(x.TelegramId.Value))
				|| (x.InviteHash != null && inviteHashes.Contains(x.InviteHash)))
			.ToListAsync(ct);

		// Сначала схлопываем дубликаты, уже существующие в БД (одна и та же сущность,
		// случайно разъехавшаяся по нескольким строкам). Это гарантирует, что словари
		// поиска ниже однозначны.
		var liveExisting = CollapseExistingDuplicates(existing);

		var byUsername = new Dictionary<string, DiscoveredChannel>(StringComparer.OrdinalIgnoreCase);
		var byTelegramId = new Dictionary<long, DiscoveredChannel>();
		var byInviteHash = new Dictionary<string, DiscoveredChannel>(StringComparer.OrdinalIgnoreCase);

		foreach (var entity in liveExisting)
		{
			AddToLookups(entity, byUsername, byTelegramId, byInviteHash);
		}

		foreach (var upsert in upserts)
		{
			var matched = new HashSet<DiscoveredChannel>();
			if (upsert.TelegramId is not null && byTelegramId.TryGetValue(upsert.TelegramId.Value, out var byTid))
				matched.Add(byTid);
			if (upsert.Username is not null && byUsername.TryGetValue(upsert.Username, out var byUn))
				matched.Add(byUn);
			if (upsert.InviteHash is not null && byInviteHash.TryGetValue(upsert.InviteHash, out var byIh))
				matched.Add(byIh);

			DiscoveredChannel target;
			if (matched.Count > 0)
			{
				target = ConsolidateDuplicates(matched.ToList());

				// Освобождаем словари от удалённых дубликатов и переиндексируем target —
				// у него могли появиться новые ключи после merge.
				foreach (var dup in matched.Where(x => x != target))
				{
					RemoveFromLookups(dup, byUsername, byTelegramId, byInviteHash);
				}

				RemoveFromLookups(target, byUsername, byTelegramId, byInviteHash);
				AddToLookups(target, byUsername, byTelegramId, byInviteHash);
			}
			else
			{
				target = new DiscoveredChannel
				{
					Id = guidFactory.New(),
					Username = upsert.Username,
					TgUrl = upsert.TgUrl,
					LastParsedId = upsert.LastParsedId,
					TelegramId = upsert.TelegramId,
					PeerType = upsert.PeerType,
					Title = upsert.Title,
					Description = upsert.Description,
					AvatarUrl = upsert.AvatarUrl,
					ParticipantsCount = upsert.ParticipantsCount,
					InviteHash = upsert.InviteHash,
					DiscoveredFromChannelId = upsert.DiscoveredFromChannelId,
					Status = DiscoveryStatus.Pending
				};
				context.DiscoveredChannels.Add(target);
				AddToLookups(target, byUsername, byTelegramId, byInviteHash);
				continue;
			}

			ApplyUpsertFields(target, upsert);
			// После ApplyUpsertFields у target мог появиться Username/InviteHash — индексируем заново.
			RemoveFromLookups(target, byUsername, byTelegramId, byInviteHash);
			AddToLookups(target, byUsername, byTelegramId, byInviteHash);
		}

		await context.SaveChangesAsync(ct);
	}

	private Task<List<DiscoveredChannel>> FindAllMatchingAsync(
		string? username,
		long? telegramId,
		string? inviteHash,
		CancellationToken ct
	)
	{
		// IgnoreQueryFilters: для корректного upsert нужно видеть и забаненные строки,
		// иначе словим duplicate key по IX_DiscoveredChannels_Username/InviteHash
		return context.DiscoveredChannels
			.IgnoreQueryFilters()
			.Where(x =>
				(telegramId != null && x.TelegramId == telegramId)
				|| (username != null && x.Username == username)
				|| (inviteHash != null && x.InviteHash == inviteHash))
			.ToListAsync(ct);
	}

	/// <summary>
	///     Сливает все полученные строки в одну (primary) и помечает остальные на удаление.
	///     Возвращает primary.
	/// </summary>
	private DiscoveredChannel ConsolidateDuplicates(IReadOnlyList<DiscoveredChannel> rows)
	{
		if (rows.Count == 1)
			return rows[0];

		var primary = ChoosePrimary(rows);
		foreach (var dup in rows)
		{
			if (ReferenceEquals(dup, primary))
				continue;
			MergeFrom(primary, dup);
			// Зануляем уникальные поля у удаляемой строки, чтобы исключить любой риск
			// нарушения IX_DiscoveredChannels_Username / IX_DiscoveredChannels_InviteHash
			// при последовательности UPDATE→DELETE внутри одной транзакции.
			dup.Username = null;
			dup.InviteHash = null;
			context.DiscoveredChannels.Remove(dup);
		}

		return primary;
	}

	private static DiscoveredChannel ChoosePrimary(IReadOnlyList<DiscoveredChannel> rows)
	{
		// Приоритет: с непустым Username → с непустым TelegramId → с непустым InviteHash → минимальный Id.
		return rows
			.OrderByDescending(x => x.Username != null)
			.ThenByDescending(x => x.TelegramId != null)
			.ThenByDescending(x => x.InviteHash != null)
			.ThenBy(x => x.Id)
			.First();
	}

	private static void AddToLookups(
		DiscoveredChannel entity,
		Dictionary<string, DiscoveredChannel> byUsername,
		Dictionary<long, DiscoveredChannel> byTelegramId,
		Dictionary<string, DiscoveredChannel> byInviteHash
	)
	{
		if (entity.Username is not null)
			byUsername[entity.Username] = entity;
		if (entity.TelegramId is not null)
			byTelegramId[entity.TelegramId.Value] = entity;
		if (entity.InviteHash is not null)
			byInviteHash[entity.InviteHash] = entity;
	}

	private static void RemoveFromLookups(
		DiscoveredChannel entity,
		Dictionary<string, DiscoveredChannel> byUsername,
		Dictionary<long, DiscoveredChannel> byTelegramId,
		Dictionary<string, DiscoveredChannel> byInviteHash
	)
	{
		if (entity.Username is not null
		    && byUsername.TryGetValue(entity.Username, out var u)
		    && ReferenceEquals(u, entity))
			byUsername.Remove(entity.Username);
		if (entity.TelegramId is not null
		    && byTelegramId.TryGetValue(entity.TelegramId.Value, out var t)
		    && ReferenceEquals(t, entity))
			byTelegramId.Remove(entity.TelegramId.Value);
		if (entity.InviteHash is not null
		    && byInviteHash.TryGetValue(entity.InviteHash, out var i)
		    && ReferenceEquals(i, entity))
			byInviteHash.Remove(entity.InviteHash);
	}

	private List<DiscoveredChannel> CollapseExistingDuplicates(List<DiscoveredChannel> rows)
	{
		if (rows.Count <= 1)
			return rows;

		// Union-Find по ключам Username / TelegramId / InviteHash: всё, что пересекается
		// хотя бы по одному ключу, попадает в одну группу.
		var parent = new int[rows.Count];
		for (var i = 0; i < parent.Length; i++)
			parent[i] = i;

		int Find(int x)
		{
			while (parent[x] != x)
			{
				parent[x] = parent[parent[x]];
				x = parent[x];
			}

			return x;
		}

		void Union(int a, int b)
		{
			var ra = Find(a);
			var rb = Find(b);
			if (ra != rb)
				parent[ra] = rb;
		}

		var idxByUsername = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var idxByTelegramId = new Dictionary<long, int>();
		var idxByInviteHash = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		for (var i = 0; i < rows.Count; i++)
		{
			var r = rows[i];
			if (r.Username is not null)
			{
				if (idxByUsername.TryGetValue(r.Username, out var j))
					Union(i, j);
				else
					idxByUsername[r.Username] = i;
			}

			if (r.TelegramId is not null)
			{
				if (idxByTelegramId.TryGetValue(r.TelegramId.Value, out var j))
					Union(i, j);
				else
					idxByTelegramId[r.TelegramId.Value] = i;
			}

			if (r.InviteHash is not null)
			{
				if (idxByInviteHash.TryGetValue(r.InviteHash, out var j))
					Union(i, j);
				else
					idxByInviteHash[r.InviteHash] = i;
			}
		}

		var groups = new Dictionary<int, List<DiscoveredChannel>>();
		for (var i = 0; i < rows.Count; i++)
		{
			var root = Find(i);
			if (!groups.TryGetValue(root, out var list))
			{
				list = [];
				groups[root] = list;
			}

			list.Add(rows[i]);
		}

		var result = new List<DiscoveredChannel>(groups.Count);
		foreach (var group in groups.Values)
		{
			result.Add(ConsolidateDuplicates(group));
		}

		return result;
	}

	private static void ApplyUpsertFields(DiscoveredChannel existing, DiscoveredPeerUpsert upsert)
	{
		if (upsert.TgUrl is not null)
			existing.TgUrl = upsert.TgUrl;
		if (upsert.LastParsedId is not null)
			existing.LastParsedId = upsert.LastParsedId;
		if (upsert.TelegramId is not null)
			existing.TelegramId = upsert.TelegramId;
		if (upsert.PeerType is not null)
			existing.PeerType = upsert.PeerType;
		if (upsert.Title is not null)
			existing.Title = upsert.Title;
		if (upsert.Description is not null)
			existing.Description = upsert.Description;
		if (upsert.AvatarUrl is not null)
			existing.AvatarUrl = upsert.AvatarUrl;
		if (upsert.ParticipantsCount is not null)
			existing.ParticipantsCount = upsert.ParticipantsCount;
		if (upsert.Username is not null && existing.Username is null)
			existing.Username = upsert.Username;
		if (upsert.InviteHash is not null && existing.InviteHash is null)
			existing.InviteHash = upsert.InviteHash;

		if (upsert.MarkAsCompleted)
		{
			existing.Status = DiscoveryStatus.Completed;
			existing.LastDiscoveredAt = DateTimeOffset.UtcNow;
		}
	}

	private static void MergeFrom(DiscoveredChannel primary, DiscoveredChannel duplicate)
	{
		// Правило: значения primary не перезатираем; забираем у дубликата только то,
		// чего у primary нет.
		primary.Username ??= duplicate.Username;
		primary.TelegramId ??= duplicate.TelegramId;
		primary.InviteHash ??= duplicate.InviteHash;
		primary.TgUrl ??= duplicate.TgUrl;
		primary.Title ??= duplicate.Title;
		primary.Description ??= duplicate.Description;
		primary.AvatarUrl ??= duplicate.AvatarUrl;
		primary.PeerType ??= duplicate.PeerType;
		primary.LastParsedId ??= duplicate.LastParsedId;
		primary.ParticipantsCount ??= duplicate.ParticipantsCount;
		primary.Category ??= duplicate.Category;
		primary.Subcategory ??= duplicate.Subcategory;
		primary.Language ??= duplicate.Language;
		primary.Tags ??= duplicate.Tags;
		primary.ClassificationConfidence ??= duplicate.ClassificationConfidence;
		primary.LastClassifiedAt ??= duplicate.LastClassifiedAt;
		primary.LastDiscoveredAt ??= duplicate.LastDiscoveredAt;
		primary.ParticipantsUpdatedAt ??= duplicate.ParticipantsUpdatedAt;
		primary.DiscoveredFromChannelId ??= duplicate.DiscoveredFromChannelId;

		if (duplicate.Status > primary.Status)
			primary.Status = duplicate.Status;
		primary.IsBanned = primary.IsBanned || duplicate.IsBanned;
	}
}