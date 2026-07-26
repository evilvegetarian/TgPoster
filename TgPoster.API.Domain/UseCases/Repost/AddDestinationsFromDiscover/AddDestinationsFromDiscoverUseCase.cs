using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

internal sealed class AddDestinationsFromDiscoverUseCase(
	IAddDestinationsFromDiscoverStorage storage,
	ITelegramChatService chatService,
	IIdentityProvider identity)
	: IRequestHandler<AddDestinationsFromDiscoverCommand, AddDestinationsFromDiscoverResponse>
{
	/// <summary>
	///     Ограничение на размер пачки: каждый канал — это резолв и вступление,
	///     на большом объёме Telegram выдаёт FLOOD_WAIT на весь аккаунт
	/// </summary>
	private const int MaxChannelsPerRequest = 20;

	public async Task<AddDestinationsFromDiscoverResponse> Handle(
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

		var candidates = await storage.GetCandidatesAsync(request.DiscoveredChannelIds, ct);
		var existingChatIds = (await storage.GetExistingChatIdsAsync(request.RepostSettingsId, ct)).ToHashSet();

		var results = new List<AddDestinationResultDto>(candidates.Count);
		var rateLimited = false;

		foreach (var candidate in candidates)
		{
			if (rateLimited)
			{
				results.Add(Result(candidate, AddDestinationOutcome.RateLimited,
					"Обработка прервана из-за ограничений Telegram"));
				continue;
			}

			var skipReason = GetSkipOutcome(candidate.TelegramId, settings.SourceChannelId, existingChatIds);
			if (skipReason != null)
			{
				results.Add(Result(candidate, skipReason.Value));
				continue;
			}

			var identifier = BuildIdentifier(candidate);
			if (identifier == null)
			{
				results.Add(Result(candidate, AddDestinationOutcome.NotResolved,
					"У канала нет ни username, ни инвайт-ссылки"));
				continue;
			}

			var chatResult = await chatService.TryGetChatInfoAsync(
				settings.TelegramSessionId,
				identifier,
				request.AutoJoin);

			if (!chatResult.IsSuccess)
			{
				if (chatResult.Status is TelegramOperationStatus.FloodWait
				    or TelegramOperationStatus.SpamRestricted)
				{
					rateLimited = true;
					results.Add(Result(candidate, AddDestinationOutcome.RateLimited, chatResult.ErrorMessage));
					continue;
				}

				results.Add(Result(candidate, AddDestinationOutcome.NotResolved, chatResult.ErrorMessage));
				continue;
			}

			var info = chatResult.Value!;
			var chatType = info.IsChannel ? ChatType.Channel
				: info.IsGroup ? ChatType.Group
				: ChatType.Unknown;

			// В Discover Telegram-id мог быть пустым или устаревшим — фиксируем актуальные данные и права
			await storage.UpdateDiscoveredChannelAsync(
				candidate.Id,
				info.Id,
				info.Title,
				info.Username,
				chatType,
				info.CanSendMessages,
				info.CanSendMedia,
				ct);

			// Повторная проверка уже по фактическому id из Telegram
			skipReason = GetSkipOutcome(info.Id, settings.SourceChannelId, existingChatIds);
			if (skipReason != null)
			{
				results.Add(Result(candidate, skipReason.Value));
				continue;
			}

			if (!info.CanSendMessages)
			{
				results.Add(Result(candidate, AddDestinationOutcome.NoWritePermission));
				continue;
			}

			if (!info.CanSendMedia)
			{
				results.Add(Result(candidate, AddDestinationOutcome.NoMediaPermission));
				continue;
			}

			// Аватарку не тянем: на пачке каналов это лишние тяжёлые запросы,
			// её подтянет обновление информации о канале
			var destinationId = await storage.AddDestinationAsync(
				request.RepostSettingsId,
				info.Id,
				info.Title,
				info.Username,
				candidate.ParticipantsCount,
				chatType,
				candidate.Id,
				settings.DefaultDelayMinSeconds,
				settings.DefaultDelayMaxSeconds,
				settings.DefaultRepostEveryNth,
				settings.DefaultSkipProbability,
				settings.DefaultMaxRepostsPerDay,
				ct);

			existingChatIds.Add(info.Id);
			results.Add(Result(candidate, AddDestinationOutcome.Added) with { DestinationId = destinationId });
		}

		// Каналы, которых нет в Discover, в candidates не попали — сообщаем о них отдельно
		var foundIds = candidates.Select(x => x.Id).ToHashSet();
		results.AddRange(request.DiscoveredChannelIds
			.Where(id => !foundIds.Contains(id))
			.Select(id => new AddDestinationResultDto
			{
				DiscoveredChannelId = id,
				Title = id.ToString(),
				Outcome = AddDestinationOutcome.NotResolved,
				Error = "Канал не найден в Discover"
			}));

		var addedCount = results.Count(x => x.Outcome == AddDestinationOutcome.Added);

		return new AddDestinationsFromDiscoverResponse
		{
			Results = results,
			AddedCount = addedCount,
			SkippedCount = results.Count - addedCount,
			RateLimited = rateLimited
		};
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

	private static string? BuildIdentifier(DiscoverCandidate candidate)
	{
		if (!string.IsNullOrWhiteSpace(candidate.Username))
		{
			return "@" + candidate.Username;
		}

		if (!string.IsNullOrWhiteSpace(candidate.InviteHash))
		{
			return "https://t.me/+" + candidate.InviteHash;
		}

		// Числовой id резолвится только по диалогам сессии — сработает,
		// если аккаунт уже состоит в канале
		return candidate.TelegramId?.ToString();
	}

	private static AddDestinationResultDto Result(
		DiscoverCandidate candidate,
		AddDestinationOutcome outcome,
		string? error = null
	) =>
		new()
		{
			DiscoveredChannelId = candidate.Id,
			Title = candidate.Title
			        ?? (candidate.Username != null ? "@" + candidate.Username : null)
			        ?? candidate.TelegramId?.ToString()
			        ?? candidate.Id.ToString(),
			Outcome = outcome,
			Error = error
		};
}
