using MassTransit;
using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

internal sealed class AddDestinationsFromDiscoverUseCase(
	IAddDestinationsFromDiscoverStorage storage,
	IBus bus,
	IIdentityProvider identity)
	: IRequestHandler<AddDestinationsFromDiscoverCommand, RepostImportJobResponse>
{
	/// <summary>
	///     Ограничение на размер пачки: каждый канал — это резолв и вступление,
	///     на большом объёме Telegram выдаёт FLOOD_WAIT на весь аккаунт
	/// </summary>
	private const int MaxChannelsPerRequest = 20;

	public async Task<RepostImportJobResponse> Handle(
		AddDestinationsFromDiscoverCommand request,
		CancellationToken ct
	)
	{
		if (request.DiscoveredChannelIds.Count == 0)
		{
			throw new InvalidRepostSettingsException("Не выбрано ни одного канала");
		}

		if (request.DiscoveredChannelIds.Count > MaxChannelsPerRequest)
		{
			throw new InvalidRepostSettingsException(
				$"За один раз можно добавить не больше {MaxChannelsPerRequest} каналов");
		}

		var settings = await storage.GetSettingsDefaultsAsync(
			request.RepostSettingsId,
			identity.Current.UserId,
			ct);
		if (settings == null)
		{
			throw new RepostSettingsNotFoundException(request.RepostSettingsId);
		}

		var requestedIds = request.DiscoveredChannelIds.Distinct().ToList();

		var candidates = (await storage.GetCandidatesAsync(requestedIds, ct))
			.ToDictionary(x => x.Id);
		var existingChatIds = (await storage.GetExistingChatIdsAsync(request.RepostSettingsId, ct)).ToHashSet();

		// Telegram здесь не трогаем: раскладываем каналы на "уже всё понятно по данным БД"
		// и "нужен резолв" — второе уедет в фоновую обработку по одному каналу
		var items = requestedIds
			.Select(id => candidates.TryGetValue(id, out var candidate)
				? BuildItem(candidate, settings.SourceChannelId, existingChatIds)
				: new ImportJobItemDto(id, id.ToString(), AddDestinationOutcome.NotResolved,
					"Канал не найден в Discover"))
			.ToList();

		var jobId = await storage.CreateImportJobAsync(
			request.RepostSettingsId,
			request.AutoJoin,
			items,
			ct);

		var pendingCount = items.Count(x => x.Outcome == AddDestinationOutcome.Pending);
		if (pendingCount > 0)
		{
			await bus.Publish(new ImportRepostDestinationsContract { JobId = jobId }, ct);
		}

		var floodWaitSeconds = GetRetryAfterSeconds(settings.SessionFloodWaitUntil);

		return new RepostImportJobResponse
		{
			JobId = jobId,
			Status = pendingCount > 0 ? RepostImportStatus.Pending : RepostImportStatus.Completed,
			TotalCount = items.Count,
			AddedCount = 0,
			SkippedCount = items.Count - pendingCount,
			PendingCount = pendingCount,
			RetryAfterSeconds = pendingCount > 0 ? floodWaitSeconds : null,
			Results = items
				.Select(x => new AddDestinationResultDto
				{
					DiscoveredChannelId = x.DiscoveredChannelId,
					Title = x.Title,
					Outcome = x.Outcome,
					Error = x.Error
				})
				.ToList()
		};
	}

	private static ImportJobItemDto BuildItem(
		DiscoverCandidate candidate,
		long sourceChannelId,
		HashSet<long> existingChatIds
	)
	{
		var title = BuildTitle(candidate);

		var skipOutcome = GetSkipOutcome(candidate.TelegramId, sourceChannelId, existingChatIds);
		if (skipOutcome != null)
		{
			return new ImportJobItemDto(candidate.Id, title, skipOutcome.Value, null);
		}

		// Права из прошлых проверок: если писать нельзя — нет смысла резолвить и вступать
		if (candidate.CanSendMessages == false)
		{
			return new ImportJobItemDto(candidate.Id, title, AddDestinationOutcome.NoWritePermission,
				"По данным последней проверки в канал нельзя писать");
		}

		if (candidate.CanSendMedia == false)
		{
			return new ImportJobItemDto(candidate.Id, title, AddDestinationOutcome.NoMediaPermission,
				"По данным последней проверки в канал нельзя отправлять медиа");
		}

		return HasIdentifier(candidate)
			? new ImportJobItemDto(candidate.Id, title, AddDestinationOutcome.Pending, null)
			: new ImportJobItemDto(candidate.Id, title, AddDestinationOutcome.NotResolved,
				"У канала нет ни username, ни инвайт-ссылки");
	}

	private static AddDestinationOutcome? GetSkipOutcome(
		long? chatId,
		long sourceChannelId,
		HashSet<long> existingChatIds
	)
	{
		if (chatId == null)
		{
			return null;
		}

		if (chatId == sourceChannelId)
		{
			return AddDestinationOutcome.SourceChannel;
		}

		return existingChatIds.Contains(chatId.Value)
			? AddDestinationOutcome.AlreadyAdded
			: null;
	}

	private static bool HasIdentifier(DiscoverCandidate candidate) =>
		!string.IsNullOrWhiteSpace(candidate.Username)
		|| !string.IsNullOrWhiteSpace(candidate.InviteHash)
		|| candidate.TelegramId != null;

	private static string BuildTitle(DiscoverCandidate candidate) =>
		candidate.Title
		?? (candidate.Username != null ? "@" + candidate.Username : null)
		?? candidate.TelegramId?.ToString()
		?? candidate.Id.ToString();

	private static int? GetRetryAfterSeconds(DateTimeOffset? floodWaitUntil)
	{
		if (floodWaitUntil == null)
		{
			return null;
		}

		var seconds = (int)Math.Ceiling((floodWaitUntil.Value - DateTimeOffset.UtcNow).TotalSeconds);

		return seconds > 0 ? seconds : null;
	}
}
