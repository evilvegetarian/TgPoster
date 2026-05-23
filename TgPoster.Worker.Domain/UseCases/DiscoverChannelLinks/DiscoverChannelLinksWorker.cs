using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

internal sealed partial class DiscoverChannelLinksWorker(
	IDiscoverChannelLinksStorage storage,
	ITelegramAuthService authService,
	ITelegramMessageService tgMessages,
	ITelegramPublicLookupService publicLookup,
	ILogger<DiscoverChannelLinksWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int MessageBatchSize = 100;
	private const int ChannelBatchSize = 1;
	private static readonly TimeSpan InterBatchDelay = TimeSpan.FromMilliseconds(1500);
	private static readonly TimeSpan InviteLookupDelay = TimeSpan.FromMilliseconds(500);
	private static readonly SemaphoreSlim ParseLock = new(1, 1);

	public async Task ProcessChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		if (!await ParseLock.WaitAsync(0, ct))
		{
			return;
		}

		try
		{
			var channels = await storage.GetChannelsToProcessAsync(ChannelBatchSize, ct);
			if (channels.Count == 0)
			{
				logger.LogInformation("Нет каналов для обработки DiscoverChannelLinks");
				return;
			}

			var sessionId = await authService.GetSessionIdForPurposeAsync(TelegramSessionPurpose.Discover, ct);
			if (sessionId is null)
			{
				return;
			}

			foreach (var channelDto in channels)
			{
				try
				{
					await ProcessChannelAsync(sessionId.Value, channelDto, ct);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Ошибка при обработке канала {Channel}",
						channelDto.Username ?? channelDto.TelegramId?.ToString());
					throw;
				}
			}
		}
		finally
		{
			ParseLock.Release();
		}
	}

	private async Task ProcessChannelAsync(
		Guid sessionId,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		var channel = await ResolveChannelAsync(sessionId, channelDto, ct);

		if (channel is null)
		{
			logger.LogWarning("Не удалось найти канал: {Channel}",
				channelDto.Username ?? channelDto.TelegramId?.ToString());
			return;
		}

		var allHistory = await GetAllHistoryAsync(sessionId, channel, channelDto.LastParsedId, ct);
		var scan = ScanChannelHistoryAsync(allHistory);

		foreach (var privId in scan.PrivateChannelIds)
		{
			scan.PrivatePeers.TryAdd(privId, new DiscoveredPeerInfo
			{
				PeerType = "channel",
				TelegramId = privId
			});
		}

		var resolvedTextPeers = await ResolveAllChatPeersAsync(scan.TextUsernames, ct);
		foreach (var resolvedPeer in resolvedTextPeers.Values)
		{
			scan.PublicPeers.TryAdd(resolvedPeer.Username!, resolvedPeer);
		}

		var resolvedInvites = await ResolveInviteLinksAsync(scan.InviteHashes, ct);

		DeduplicateResolvedInvites(scan.PublicPeers, scan.PrivatePeers, resolvedInvites);
		DeduplicateByTitle(scan.PublicPeers, scan.PrivatePeers, resolvedInvites);

		if (channelDto.Username is not null)
		{
			scan.PublicPeers.Remove(channelDto.Username);
		}

		logger.LogInformation(
			"Найдено {PublicCount} публичных, {PrivateCount} приватных, {InviteCount} по инвайтам в {Channel}",
			scan.PublicPeers.Count, scan.PrivatePeers.Count, resolvedInvites.Count,
			channelDto.Username ?? channelDto.TelegramId?.ToString());

		await SaveDiscoveredPeersAsync(channelDto, channel, scan, resolvedInvites, ct);
	}

	private async Task<TelegramChatInfo?> ResolveChannelAsync(
		Guid sessionId,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		if (!string.IsNullOrEmpty(channelDto.Username))
		{
			logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);
			var resolved = await tgMessages.ResolveChannelAsync(sessionId, channelDto.Username, ct);

			if (await resolved.HandleChannelUnavailableAsync(() => storage.ChannelBanned(channelDto.Id, ct)))
			{
				logger.LogError("Канал {channel} забанен", channelDto.Username);
				return null;
			}

			if (resolved.IsSuccess)
			{
				return resolved.Value;
			}

			logger.LogError("Не удалось разрешить канал {Channel}: {Status} {Error}",
				channelDto.Username, resolved.Status, resolved.ErrorMessage);

			return null;
		}

		if (channelDto.TelegramId.HasValue)
		{
			logger.LogInformation("Поиск TG-ссылок в приватном канале ID={TelegramId}", channelDto.TelegramId);
			var dialogsResult = await tgMessages.GetAllDialogsAsync(sessionId, ct);
			if (!dialogsResult.IsSuccess)
			{
				logger.LogError("Не удалось получить диалоги: {Status} {Error}",
					dialogsResult.Status, dialogsResult.ErrorMessage);
				return null;
			}

			return dialogsResult.Value!.FirstOrDefault(c => c.Id == channelDto.TelegramId.Value);
		}

		logger.LogDebug("Пропускаем канал только с инвайт-хешем — нет доступа для сканирования");
		return null;
	}

	private async Task<List<TelegramHistoryPage>> GetAllHistoryAsync(
		Guid sessionId,
		TelegramChatInfo channel,
		int? lastParsedId,
		CancellationToken ct
	)
	{
		var offset = 0;
		var transientAttempts = 0;
		const int maxTransientAttempts = 3;
		var allHistory = new List<TelegramHistoryPage>();
		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var historyResult = await tgMessages.SearchMessagesAsync(
				sessionId,
				channel.Peer,
				filter: TelegramMessageFilter.Url,
				limit: MessageBatchSize,
				offsetId: offset,
				minId: lastParsedId ?? 0,
				ct: ct);

			if (!historyResult.IsSuccess)
			{
				logger.LogWarning("Ошибка получения истории канала {Channel}: {Status} {Error}", channel.Username,
					historyResult.Status, historyResult.ErrorMessage);

				if (historyResult.Status is TelegramOperationStatus.Timeout or TelegramOperationStatus.UnknownError)
				{
					if (++transientAttempts >= maxTransientAttempts)
					{
						logger.LogWarning("Прерываем сканирование {Channel}: исчерпан лимит ретраев ({Attempts})",
							channel.Username, transientAttempts);
						break;
					}

					await Task.Delay(TimeSpan.FromSeconds(5), ct);
					continue;
				}

				break;
			}

			transientAttempts = 0;

			var history = historyResult.Value!;
			allHistory.Add(history);

			if (history.Messages.Count == 0)
			{
				break;
			}

			offset = history.Messages[^1].Id;
			if (history.Messages.Count < MessageBatchSize)
			{
				break;
			}

			await Task.Delay(InterBatchDelay, ct);
		}

		return allHistory;
	}

	private HistoryScanResult ScanChannelHistoryAsync(List<TelegramHistoryPage> allHistory)
	{
		var publicPeers = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);
		var privatePeers = new Dictionary<long, DiscoveredPeerInfo>();
		var textUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var inviteHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var privateChannelIds = new HashSet<long>();

		foreach (var page in allHistory)
		{
			foreach (var chat in page.Chats)
			{
				var peerType = ResolvePeerType(chat);

				if (!string.IsNullOrEmpty(chat.Username))
				{
					publicPeers.TryAdd(chat.Username, new DiscoveredPeerInfo
					{
						PeerType = peerType,
						Username = chat.Username,
						TelegramId = chat.Id,
						Title = chat.Title,
						ParticipantsCount = chat.ParticipantsCount
					});
				}
				else if (chat.Id != 0)
				{
					privatePeers.TryAdd(chat.Id, new DiscoveredPeerInfo
					{
						PeerType = peerType,
						TelegramId = chat.Id,
						Title = chat.Title,
						ParticipantsCount = chat.ParticipantsCount
					});
				}
			}
		}

		var allMessages = allHistory.SelectMany(page => page.Messages).ToList();
		var lastParseId = allMessages.Count > 0 ? allMessages.Max(x => x.Id) : 0;

		foreach (var message in allMessages)
		{
			ExtractLinksFromText(message.Text, textUsernames, inviteHashes, privateChannelIds);
			ExtractLinksFromEntities(message.Entities, textUsernames, inviteHashes, privateChannelIds);
		}

		// HTTP-lookup делаем только для username'ов, которых нет в chats — там данные мы уже взяли бесплатно из MTProto
		textUsernames.ExceptWith(publicPeers.Keys);

		return new HistoryScanResult(publicPeers, privatePeers, textUsernames, inviteHashes, privateChannelIds,
			lastParseId);
	}

	private async Task SaveDiscoveredPeersAsync(
		DiscoverChannelDto channelDto,
		TelegramChatInfo channel,
		HistoryScanResult scan,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites,
		CancellationToken ct
	)
	{
		var batch = new List<DiscoveredPeerUpsert>(
			scan.PublicPeers.Count + scan.PrivatePeers.Count + resolvedInvites.Count);

		foreach (var peer in scan.PublicPeers.Values)
		{
			batch.Add(new DiscoveredPeerUpsert
			{
				Username = peer.Username,
				TgUrl = $"https://t.me/{peer.Username}",
				TelegramId = peer.TelegramId != 0 ? peer.TelegramId : null,
				PeerType = peer.PeerType,
				Title = peer.Title,
				Description = peer.Description,
				AvatarUrl = peer.AvatarUrl,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = peer.InviteHash,
				DiscoveredFromChannelId = channelDto.Id
			});
		}

		foreach (var peer in scan.PrivatePeers.Values)
		{
			batch.Add(new DiscoveredPeerUpsert
			{
				TelegramId = peer.TelegramId,
				PeerType = peer.PeerType,
				Title = peer.Title,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = peer.InviteHash,
				DiscoveredFromChannelId = channelDto.Id
			});
		}

		foreach (var (hash, peer) in resolvedInvites)
		{
			batch.Add(new DiscoveredPeerUpsert
			{
				Username = peer.Username,
				TgUrl = $"https://t.me/+{hash}",
				TelegramId = peer.TelegramId != 0 ? peer.TelegramId : null,
				PeerType = peer.PeerType,
				Title = peer.Title,
				Description = peer.Description,
				AvatarUrl = peer.AvatarUrl,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = hash,
				DiscoveredFromChannelId = channelDto.Id
			});
		}

		await storage.BulkUpsertAsync(batch, ct);

		await storage.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = channelDto.Username,
			LastParsedId = scan.LastParsedId,
			TelegramId = channel.Id,
			PeerType = ResolvePeerType(channel),
			Title = channel.Title,
			ParticipantsCount = channel.ParticipantsCount,
			MarkAsCompleted = true
		}, ct);
	}

	private async Task<Dictionary<string, DiscoveredPeerInfo>> ResolveAllChatPeersAsync(
		HashSet<string> usernames,
		CancellationToken ct
	)
	{
		var chats = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var username in usernames)
		{
			ct.ThrowIfCancellationRequested();

			var peerInfo = await ResolveChatPeersAsync(username, ct);
			if (peerInfo.HasValue)
			{
				chats.TryAdd(username, peerInfo.Value);
			}

			await Task.Delay(TimeSpan.FromSeconds(10), ct);
		}

		return chats;
	}

	private async Task<DiscoveredPeerInfo?> ResolveChatPeersAsync(
		string username,
		CancellationToken ct
	)
	{
		var result = await publicLookup.LookupAsync(username, ct);
		if (!result.IsSuccess || result.Value is null)
		{
			logger.LogDebug("HTTP-lookup не удался для @{Username} ({Status}), пропускаем",
				username, result.Status);
			return null;
		}

		var info = result.Value;
		if (info.Type is not (TelegramEntityType.Channel or TelegramEntityType.Group))
		{
			return null;
		}

		return new DiscoveredPeerInfo
		{
			PeerType = info.Type == TelegramEntityType.Channel ? "channel" : "chat",
			Username = info.Username,
			Title = info.Title,
			Description = info.Description,
			AvatarUrl = info.PhotoUrl,
			ParticipantsCount = info.MembersCount is { } members
				? (int?)Math.Min(members, int.MaxValue)
				: null
		};
	}

	private async Task<Dictionary<string, DiscoveredPeerInfo>> ResolveInviteLinksAsync(
		HashSet<string> hashes,
		CancellationToken ct
	)
	{
		var results = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var hash in hashes)
		{
			ct.ThrowIfCancellationRequested();

			var result = await publicLookup.LookupInviteAsync(hash, ct);
			if (!result.IsSuccess || result.Value is null)
			{
				logger.LogDebug("HTTP-lookup инвайта {Hash} не удался ({Status}), пропускаем",
					hash, result.Status);
				await Task.Delay(InviteLookupDelay, ct);
				continue;
			}

			var info = result.Value;
			if (info.Type is not (TelegramEntityType.Channel or TelegramEntityType.Group))
			{
				await Task.Delay(InviteLookupDelay, ct);
				continue;
			}

			results[hash] = new DiscoveredPeerInfo
			{
				PeerType = info.Type == TelegramEntityType.Channel ? "channel" : "chat",
				Username = info.Username,
				TelegramId = 0,
				Title = info.Title,
				Description = info.Description,
				AvatarUrl = info.PhotoUrl,
				ParticipantsCount = info.MembersCount is { } members
					? (int?)Math.Min(members, int.MaxValue)
					: null,
				InviteHash = hash
			};

			await Task.Delay(InviteLookupDelay, ct);
		}

		return results;
	}

	private static void DeduplicateResolvedInvites(
		Dictionary<string, DiscoveredPeerInfo> allUsernames,
		Dictionary<long, DiscoveredPeerInfo> privateChats,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites
	)
	{
		var hashesToRemove = new List<string>();
		var seenTelegramIds = new HashSet<long>();

		foreach (var (hash, peer) in resolvedInvites)
		{
			if (peer.Username is not null && allUsernames.TryGetValue(peer.Username, out var existingByUsername))
			{
				if (existingByUsername.InviteHash is null)
				{
					allUsernames[peer.Username] = existingByUsername with { InviteHash = hash };
				}

				hashesToRemove.Add(hash);
				continue;
			}

			if (peer.TelegramId != 0 && privateChats.TryGetValue(peer.TelegramId, out var existingPrivate))
			{
				if (peer.Username is not null)
				{
					allUsernames.TryAdd(peer.Username, peer);
					privateChats.Remove(peer.TelegramId);
				}
				else if (existingPrivate.InviteHash is null)
				{
					privateChats[peer.TelegramId] = existingPrivate with { InviteHash = hash };
				}

				hashesToRemove.Add(hash);
				continue;
			}

			if (peer.TelegramId != 0 && !seenTelegramIds.Add(peer.TelegramId))
			{
				hashesToRemove.Add(hash);
			}
		}

		foreach (var hash in hashesToRemove)
		{
			resolvedInvites.Remove(hash);
		}
	}

	private static void DeduplicateByTitle(
		Dictionary<string, DiscoveredPeerInfo> allUsernames,
		Dictionary<long, DiscoveredPeerInfo> privateChats,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites
	)
	{
		var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var peer in allUsernames.Values)
		{
			if (peer.Title is not null)
			{
				seenTitles.Add(peer.Title);
			}
		}

		var idsToRemove = new List<long>();
		foreach (var (id, peer) in privateChats)
		{
			if (peer.Title is not null && !seenTitles.Add(peer.Title))
			{
				idsToRemove.Add(id);
			}
		}

		foreach (var id in idsToRemove)
		{
			privateChats.Remove(id);
		}

		var hashesToRemove = new List<string>();
		foreach (var (hash, peer) in resolvedInvites)
		{
			if (peer.Username is null && peer.Title is not null && !seenTitles.Add(peer.Title))
			{
				hashesToRemove.Add(hash);
			}
		}

		foreach (var hash in hashesToRemove)
		{
			resolvedInvites.Remove(hash);
		}
	}

	private static string ResolvePeerType(TelegramChatInfo chat) => chat.IsChannel ? "channel" : "chat";

	private static void ExtractLinksFromEntities(
		IReadOnlyList<TelegramMessageEntity>? entities,
		HashSet<string> usernames,
		HashSet<string> inviteHashes,
		HashSet<long> privateChannelIds
	)
	{
		if (entities is null)
		{
			return;
		}

		foreach (var entity in entities)
		{
			if (entity.Type != TelegramMessageEntityType.TextUrl || entity.Url is null)
			{
				continue;
			}

			ExtractLinksFromText(entity.Url, usernames, inviteHashes, privateChannelIds);
		}
	}

	private static void ExtractLinksFromText(
		string? text,
		HashSet<string> usernames,
		HashSet<string> inviteHashes,
		HashSet<long> privateChannelIds
	)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		foreach (Match match in TmeLinkRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
			{
				usernames.Add(username);
			}
		}

		foreach (Match match in MentionRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
			{
				usernames.Add(username);
			}
		}

		foreach (Match match in InviteLinkRegex().Matches(text))
		{
			inviteHashes.Add(match.Groups[1].Value);
		}

		foreach (Match match in PrivateChannelLinkRegex().Matches(text))
		{
			if (long.TryParse(match.Groups[1].Value, out var id))
			{
				privateChannelIds.Add(id);
			}
		}
	}

	[GeneratedRegex(@"(?:https?://)?t\.me/(?!(?:joinchat/|\+|c/))([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex TmeLinkRegex();

	[GeneratedRegex(@"@([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex MentionRegex();

	[GeneratedRegex(@"(?:https?://)?t\.me/(?:joinchat/|\+)([\w\-]+)", RegexOptions.Compiled)]
	private static partial Regex InviteLinkRegex();

	[GeneratedRegex(@"(?:https?://)?t\.me/c/(\d+)(?:/\d+)?", RegexOptions.Compiled)]
	private static partial Regex PrivateChannelLinkRegex();

	private sealed record HistoryScanResult(
		Dictionary<string, DiscoveredPeerInfo> PublicPeers,
		Dictionary<long, DiscoveredPeerInfo> PrivatePeers,
		HashSet<string> TextUsernames,
		HashSet<string> InviteHashes,
		HashSet<long> PrivateChannelIds,
		int LastParsedId);

	private readonly record struct DiscoveredPeerInfo(
		string? Username = null,
		long TelegramId = 0,
		string PeerType = "channel",
		string? Title = null,
		int? ParticipantsCount = null,
		string? InviteHash = null,
		string? Description = null,
		string? AvatarUrl = null);
}
