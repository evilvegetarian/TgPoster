using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

internal sealed partial class DiscoverChannelLinksWorker(
	IDiscoverChannelLinksStorage storage,
	ITelegramAuthService authService,
	IWorkerJobStatusStorage statusStorage,
	IServiceScopeFactory scopeFactory,
	HangfireNextRunProvider nextRun,
	ILogger<DiscoverChannelLinksWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int MessageBatchSize = 100;
	private const int ChannelsPerSession = 1;
	private static readonly TimeSpan InterBatchDelay = TimeSpan.FromMilliseconds(1500);
	private static readonly TimeSpan InviteLookupDelay = TimeSpan.FromMilliseconds(500);
	private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(20);
	private static readonly SemaphoreSlim ParseLock = new(1, 1);

	// Ветки разных сессий работают параллельно, но storage и statusStorage живут в одном
	// scope и делят один DbContext — обращения к нему сериализуем
	private readonly SemaphoreSlim dbLock = new(1, 1);

	private long lastHeartbeatTicks;
	private int progressCurrent;
	private int progressTotal;

	public async Task ProcessChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		if (!await ParseLock.WaitAsync(0, ct))
		{
			return;
		}

		try
		{
			await TryReportAsync(() => statusStorage.ReportStartedAsync(WorkerJobNames.DiscoverChannelLinks, ct));

			var outcome = await ProcessChannelsCoreAsync(ct);

			// Финальную запись статуса делаем с CancellationToken.None: при остановке приложения
			// она должна успеть выполниться best-effort
			var nextRunAt = nextRun.GetNextRunAt(WorkerJobNames.DiscoverChannelLinks);
			if (outcome.CooldownSeconds is { } floodWait)
			{
				await TryReportAsync(() => statusStorage.ReportCooldownAsync(
					WorkerJobNames.DiscoverChannelLinks,
					DateTimeOffset.UtcNow.AddSeconds(floodWait),
					nextRunAt,
					CancellationToken.None));
			}
			else if (outcome.Error is { } error)
			{
				await TryReportAsync(() => statusStorage.ReportFailedAsync(
					WorkerJobNames.DiscoverChannelLinks, error, nextRunAt, CancellationToken.None));
			}
			else
			{
				await TryReportAsync(() => statusStorage.ReportCompletedAsync(
					WorkerJobNames.DiscoverChannelLinks, nextRunAt, CancellationToken.None));
			}
		}
		catch (OperationCanceledException)
		{
			// Статус не пишем: запись останется Running, и API покажет «прервана» по протухшему heartbeat'у
			throw;
		}
		catch (Exception ex)
		{
			await TryReportAsync(() => statusStorage.ReportFailedAsync(
				WorkerJobNames.DiscoverChannelLinks,
				ex.Message,
				nextRun.GetNextRunAt(WorkerJobNames.DiscoverChannelLinks),
				CancellationToken.None));
			throw;
		}
		finally
		{
			ParseLock.Release();
		}
	}

	/// <summary>
	///     Разложить каналы по доступным Discover-сессиям и обработать их параллельно:
	///     за каждой сессией стоит отдельный Telegram-аккаунт со своими лимитами
	/// </summary>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Кулдаун, если FloodWait поймали все сессии; ошибка, если упали все каналы запуска</returns>
	private async Task<RunOutcome> ProcessChannelsCoreAsync(CancellationToken ct)
	{
		var sessionIds = await authService.GetSessionIdsForPurposeAsync(TelegramSessionPurpose.Discover, ct);
		if (sessionIds.Count == 0)
		{
			logger.LogWarning("Нет активных Telegram-сессий с назначением Discover");
			return RunOutcome.Success;
		}

		var channels = await storage.GetChannelsToProcessAsync(ChannelsPerSession * sessionIds.Count, ct);
		if (channels.Count == 0)
		{
			logger.LogInformation("Нет каналов для обработки DiscoverChannelLinks");
			return RunOutcome.Success;
		}

		progressTotal = channels.Count;
		progressCurrent = 0;

		// Раскладываем каналы по сессиям round-robin: если каналов меньше, чем сессий,
		// лишние аккаунты просто не задействуются
		var buckets = channels
			.Select((channel, index) => (channel, sessionId: sessionIds[index % sessionIds.Count]))
			.GroupBy(x => x.sessionId, x => x.channel)
			.ToList();

		logger.LogInformation("DiscoverChannelLinks: {ChannelCount} каналов на {SessionCount} сессий",
			channels.Count, buckets.Count);

		await ReportProgressAsync($"Обработка {channels.Count} каналов в {buckets.Count} потоков", ct, true);

		var tasks = buckets
			.Select(bucket => ProcessSessionChannelsAsync(bucket.Key, bucket.ToList(), ct))
			.ToArray();

		var sessionOutcomes = await Task.WhenAll(tasks);

		// Кулдаун имеет смысл, только если FloodWait поймали все сессии: пока свободен
		// хотя бы один аккаунт, следующий запуск снова принесёт результат
		if (sessionOutcomes.All(x => x.FloodWaitSeconds is not null))
		{
			return RunOutcome.Cooldown(sessionOutcomes.Min(x => x.FloodWaitSeconds!.Value));
		}

		// Единичный сбой — норма (канал мог сломаться), а вот если упали все каналы запуска,
		// скорее всего сломано что-то общее: сессии, сеть или БД
		var failedCount = sessionOutcomes.Sum(x => x.FailedCount);
		return failedCount == channels.Count
			? RunOutcome.Failed(sessionOutcomes.Select(x => x.LastError).LastOrDefault(x => x is not null))
			: RunOutcome.Success;
	}

	/// <summary>
	///     Последовательно обработать каналы, закреплённые за одной сессией
	/// </summary>
	/// <param name="sessionId">ID сессии Telegram</param>
	/// <param name="channels">Каналы этой сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>FloodWait, если Telegram ограничил сессию, и число каналов, на которых обработка упала</returns>
	private async Task<SessionOutcome> ProcessSessionChannelsAsync(
		Guid sessionId,
		IReadOnlyList<DiscoverChannelDto> channels,
		CancellationToken ct
	)
	{
		// Свой scope на ветку: Telegram-слой ходит в БД за данными сессии, а один DbContext
		// нельзя использовать из нескольких потоков одновременно
		using var scope = scopeFactory.CreateScope();
		var session = new SessionScope(
			sessionId,
			scope.ServiceProvider.GetRequiredService<ITelegramMessageService>(),
			scope.ServiceProvider.GetRequiredService<ITelegramPublicLookupService>());

		var failedCount = 0;
		string? lastError = null;

		foreach (var channelDto in channels)
		{
			await ReportProgressAsync($"@{channelDto.Username ?? channelDto.TelegramId?.ToString()}", ct);

			int? floodWaitSeconds = null;
			try
			{
				floodWaitSeconds = await ProcessChannelAsync(session, channelDto, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// Без пометки канал остался бы первым в очереди и ронял бы каждый следующий запуск
				logger.LogError(ex, "Ошибка при обработке канала {Channel} сессией {SessionId}",
					channelDto.Username ?? channelDto.TelegramId?.ToString(), sessionId);
				await WithDbLockAsync(() => storage.MarkAsErrorAsync(channelDto.Id, ct), ct);
				failedCount++;
				lastError = ex.Message;
			}

			Interlocked.Increment(ref progressCurrent);
			if (floodWaitSeconds is not null)
			{
				return new SessionOutcome(floodWaitSeconds, failedCount, lastError);
			}
		}

		return new SessionOutcome(null, failedCount, lastError);
	}

	/// <summary>
	///     Обработать один канал: найти ссылки в истории и сохранить обнаруженные пиры
	/// </summary>
	/// <param name="session">Telegram-сервисы ветки вместе с ID сессии</param>
	/// <param name="channelDto">Канал для обработки</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Секунды FloodWait-таймаута, если Telegram его вернул, иначе null</returns>
	private async Task<int?> ProcessChannelAsync(
		SessionScope session,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		var resolution = await ResolveChannelAsync(session, channelDto, ct);
		if (resolution.FloodWaitSeconds is not null)
		{
			return resolution.FloodWaitSeconds;
		}

		var channel = resolution.Channel;
		if (channel is null)
		{
			logger.LogWarning("Не удалось найти канал: {Channel}",
				channelDto.Username ?? channelDto.TelegramId?.ToString());
			return null;
		}

		var fetch = await GetAllHistoryAsync(session, channel, channelDto.LastParsedId, ct);
		var scan = ScanChannelHistoryAsync(fetch.Pages);

		foreach (var privId in scan.PrivateChannelIds)
		{
			scan.PrivatePeers.TryAdd(privId, new DiscoveredPeerInfo
			{
				PeerType = "channel",
				TelegramId = privId
			});
		}

		var resolvedTextPeers = await ResolveAllChatPeersAsync(session, scan.TextUsernames, ct);
		foreach (var resolvedPeer in resolvedTextPeers.Values)
		{
			scan.PublicPeers.TryAdd(resolvedPeer.Username!, resolvedPeer);
		}

		var resolvedInvites = await ResolveInviteLinksAsync(session, scan.InviteHashes, ct);

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

		return fetch.FloodWaitSeconds;
	}

	/// <summary>
	///     Найти канал в Telegram. Канал, который не удалось открыть по его собственной вине, помечается
	///     ошибкой, иначе он оставался бы первым в очереди и занимал сессию на каждом запуске
	/// </summary>
	/// <param name="session"></param>
	/// <param name="channelDto"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<ChannelResolution> ResolveChannelAsync(
		SessionScope session,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		if (!string.IsNullOrEmpty(channelDto.Username))
		{
			logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);
			var resolved = await session.Messages.ResolveChannelAsync(session.SessionId, channelDto.Username, ct);

			if (await resolved.HandleChannelUnavailableAsync(
				    () => WithDbLockAsync(() => storage.ChannelBanned(channelDto.Id, ct), ct)))
			{
				logger.LogError("Канал {channel} забанен", channelDto.Username);
				return ChannelResolution.NotFound;
			}

			if (resolved.IsSuccess)
			{
				return ChannelResolution.Found(resolved.Value!);
			}

			if (resolved.Status is TelegramOperationStatus.FloodWait)
			{
				logger.LogWarning("FloodWait {Seconds} с при поиске канала @{Channel}",
					resolved.FloodWaitSeconds, channelDto.Username);
				return ChannelResolution.FloodWait(resolved.FloodWaitSeconds ?? 0);
			}

			logger.LogError("Не удалось разрешить канал {Channel}: {Status} {Error}",
				channelDto.Username, resolved.Status, resolved.ErrorMessage);

			// Таймаут и проблемы самой сессии канал не касаются — он останется в очереди на следующий запуск
			if (resolved.Status is not (TelegramOperationStatus.Timeout
			    or TelegramOperationStatus.SessionNotFound
			    or TelegramOperationStatus.SpamRestricted))
			{
				await WithDbLockAsync(() => storage.MarkAsErrorAsync(channelDto.Id, ct), ct);
			}

			return ChannelResolution.NotFound;
		}

		if (channelDto.TelegramId.HasValue)
		{
			logger.LogInformation("Поиск TG-ссылок в приватном канале ID={TelegramId}", channelDto.TelegramId);
			var dialogsResult = await session.Messages.GetAllDialogsAsync(session.SessionId, ct);
			if (!dialogsResult.IsSuccess)
			{
				logger.LogError("Не удалось получить диалоги: {Status} {Error}",
					dialogsResult.Status, dialogsResult.ErrorMessage);
				return ChannelResolution.NotFound;
			}

			var dialog = dialogsResult.Value!.FirstOrDefault(c => c.Id == channelDto.TelegramId.Value);
			return dialog is null ? ChannelResolution.NotFound : ChannelResolution.Found(dialog);
		}

		logger.LogDebug("Пропускаем канал только с инвайт-хешем — нет доступа для сканирования");
		return ChannelResolution.NotFound;
	}

	private async Task<HistoryFetchResult> GetAllHistoryAsync(
		SessionScope session,
		TelegramChatInfo channel,
		int? lastParsedId,
		CancellationToken ct
	)
	{
		var offset = 0;
		var loadedMessages = 0;
		var transientAttempts = 0;
		const int maxTransientAttempts = 3;
		var allHistory = new List<TelegramHistoryPage>();
		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var historyResult = await session.Messages.SearchMessagesAsync(
				session.SessionId,
				channel.Peer,
				TelegramMessageFilter.Url,
				MessageBatchSize,
				offset,
				lastParsedId ?? 0,
				ct: ct);

			if (!historyResult.IsSuccess)
			{
				logger.LogWarning("Ошибка получения истории канала {Channel}: {Status} {Error}", channel.Username,
					historyResult.Status, historyResult.ErrorMessage);

				// Длинный FloodWait пробрасываем наружу: уже собраные страницы обработаем,
				// а время окончания таймаута попадёт в статус задачи
				if (historyResult.Status is TelegramOperationStatus.FloodWait)
				{
					return new HistoryFetchResult(allHistory, historyResult.FloodWaitSeconds);
				}

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
			loadedMessages += history.Messages.Count;

			await ReportProgressAsync(
				$"@{channel.Username ?? channel.Title}: загружено {loadedMessages} сообщений", ct);

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

		return new HistoryFetchResult(allHistory, null);
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

		// Обе записи делаем под одной блокировкой: пока идёт запись, соседняя ветка
		// не должна читать наполовину сохранённую картину и плодить дубли
		await WithDbLockAsync(async () =>
		{
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
		}, ct);
	}

	private async Task<Dictionary<string, DiscoveredPeerInfo>> ResolveAllChatPeersAsync(
		SessionScope session,
		HashSet<string> usernames,
		CancellationToken ct
	)
	{
		var chats = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var username in usernames)
		{
			ct.ThrowIfCancellationRequested();

			await ReportProgressAsync($"HTTP-lookup @{username}", ct);

			var peerInfo = await ResolveChatPeersAsync(session, username, ct);
			if (peerInfo.HasValue)
			{
				chats.TryAdd(username, peerInfo.Value);
			}

			await Task.Delay(TimeSpan.FromSeconds(10), ct);
		}

		return chats;
	}

	private async Task<DiscoveredPeerInfo?> ResolveChatPeersAsync(
		SessionScope session,
		string username,
		CancellationToken ct
	)
	{
		var result = await session.PublicLookup.LookupAsync(username, ct);
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
		SessionScope session,
		HashSet<string> hashes,
		CancellationToken ct
	)
	{
		var results = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var hash in hashes)
		{
			ct.ThrowIfCancellationRequested();

			await ReportProgressAsync($"HTTP-lookup инвайта {hash}", ct);

			var result = await session.PublicLookup.LookupInviteAsync(hash, ct);
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

	/// <summary>
	///     Обновить heartbeat и прогресс задачи в хранилище статусов. Записи троттлятся,
	///     чтобы не спамить БД из частых циклов; вызывается из параллельных веток, поэтому
	///     отметка последней записи обновляется атомарно
	/// </summary>
	/// <param name="message">Человекочитаемое описание текущего этапа</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="force">Записать без учёта троттлинга</param>
	private async Task ReportProgressAsync(string? message, CancellationToken ct, bool force = false)
	{
		var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
		if (force)
		{
			Interlocked.Exchange(ref lastHeartbeatTicks, nowTicks);
		}
		else
		{
			// Право на запись получает та ветка, которая первой перебила отметку:
			// остальные в этом окне просто выходят
			var last = Interlocked.Read(ref lastHeartbeatTicks);
			if (nowTicks - last < HeartbeatInterval.Ticks
			    || Interlocked.CompareExchange(ref lastHeartbeatTicks, nowTicks, last) != last)
			{
				return;
			}
		}

		await TryReportAsync(() => WithDbLockAsync(
			() => statusStorage.ReportHeartbeatAsync(
				WorkerJobNames.DiscoverChannelLinks,
				Volatile.Read(ref progressCurrent),
				progressTotal,
				message,
				ct),
			ct));
	}

	/// <summary>
	///     Выполнить обращение к БД под общей блокировкой: параллельные ветки делят
	///     один DbContext, а он не расчитан на одновременное использование
	/// </summary>
	/// <param name="action">Операция с хранилищем</param>
	/// <param name="ct">Токен отмены</param>
	private async Task WithDbLockAsync(Func<Task> action, CancellationToken ct)
	{
		await dbLock.WaitAsync(ct);
		try
		{
			await action();
		}
		finally
		{
			dbLock.Release();
		}
	}

	/// <summary>
	///     Выполнить запись статуса, проглатывая ошибки: сбой записи не должен ронять job
	/// </summary>
	/// <param name="report">Операция записи статуса</param>
	private async Task TryReportAsync(Func<Task> report)
	{
		try
		{
			await report();
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Не удалось записать статус задачи {JobName}",
				WorkerJobNames.DiscoverChannelLinks);
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
			var hash = match.Groups[1].Value;

			// t.me/+79633081622 — диплинк на пользователя по номеру телефона, а не инвайт:
			// настоящие инвайт-хеши всегда содержат буквы
			if (hash.All(char.IsAsciiDigit))
			{
				continue;
			}

			inviteHashes.Add(hash);
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

	/// <summary>
	///     Telegram-сервисы одной ветки вместе с сессией, от имени которой она работает.
	///     Живут в собственном DI-scope, поэтому не пересекаются с соседними ветками
	/// </summary>
	/// <param name="SessionId">ID сессии Telegram</param>
	/// <param name="Messages">Сервис работы с сообщениями</param>
	/// <param name="PublicLookup">Сервис HTTP-lookup публичных страниц t.me</param>
	private sealed record SessionScope(
		Guid SessionId,
		ITelegramMessageService Messages,
		ITelegramPublicLookupService PublicLookup);

	private sealed record HistoryFetchResult(
		List<TelegramHistoryPage> Pages,
		int? FloodWaitSeconds);

	/// <summary>
	///     Итог запуска: успех, ошибка (упали все каналы) или кулдаун (FloodWait у всех сессий)
	/// </summary>
	/// <param name="Error"></param>
	/// <param name="CooldownSeconds"></param>
	private sealed record RunOutcome(string? Error, int? CooldownSeconds)
	{
		public static RunOutcome Success { get; } = new(null, null);

		public static RunOutcome Failed(string? error) => new(error, null);

		public static RunOutcome Cooldown(int seconds) => new(null, seconds);
	}

	/// <summary>
	///     Итог работы одной сесии за запуск
	/// </summary>
	/// <param name="FloodWaitSeconds"></param>
	/// <param name="FailedCount"></param>
	/// <param name="LastError"></param>
	private sealed record SessionOutcome(int? FloodWaitSeconds, int FailedCount, string? LastError);

	/// <summary>
	///     Результат поиска канала: сам канал либо FloodWait, из-за которого сессию надо остановить
	/// </summary>
	/// <param name="Channel"></param>
	/// <param name="FloodWaitSeconds"></param>
	private sealed record ChannelResolution(TelegramChatInfo? Channel, int? FloodWaitSeconds)
	{
		public static ChannelResolution NotFound { get; } = new(null, null);

		public static ChannelResolution Found(TelegramChatInfo channel) => new(channel, null);

		public static ChannelResolution FloodWait(int seconds) => new(null, seconds);
	}

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