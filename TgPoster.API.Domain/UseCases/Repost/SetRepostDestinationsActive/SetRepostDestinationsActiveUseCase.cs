using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;

internal sealed class SetRepostDestinationsActiveUseCase(
	ISetRepostDestinationsActiveStorage storage,
	ITelegramChatService chatService,
	IIdentityProvider identity)
	: IRequestHandler<SetRepostDestinationsActiveCommand, SetRepostDestinationsActiveResponse>
{
	public async Task<SetRepostDestinationsActiveResponse> Handle(
		SetRepostDestinationsActiveCommand request,
		CancellationToken ct
	)
	{
		var sessionId = await storage.GetTelegramSessionIdAsync(request.SettingsId, identity.Current.UserId, ct);
		if (sessionId is null)
			throw new RepostSettingsNotFoundException(request.SettingsId);

		var ids = request.DestinationIds.Distinct().ToList();
		var destinations = await storage.GetDestinationsAsync(request.SettingsId, ids, ct);

		var foundIds = destinations.Select(d => d.Id).ToHashSet();
		var missingId = ids.Where(id => !foundIds.Contains(id)).Select(id => (Guid?)id).FirstOrDefault();
		if (missingId.HasValue)
			throw new RepostDestinationNotFoundException(missingId.Value);

		return request.IsActive
			? await EnableAsync(sessionId.Value, destinations, ct)
			: await DisableAsync(destinations, ct);
	}

	private async Task<SetRepostDestinationsActiveResponse> DisableAsync(
		List<RepostDestinationState> destinations,
		CancellationToken ct
	)
	{
		var ids = destinations.Where(d => d.IsActive).Select(d => d.Id).ToList();
		if (ids.Count > 0)
			await storage.DeactivateAsync(ids, ct);

		return Result(ids.Count, 0, 0);
	}

	/// <summary>
	/// Включает только каналы, где аккаунт уже состоит и может пересылать медиа. В чаты не вступает:
	/// сотня вступлений подряд упирается в FloodWait, такие каналы включаются по одному
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="destinations"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<SetRepostDestinationsActiveResponse> EnableAsync(
		Guid sessionId,
		List<RepostDestinationState> destinations,
		CancellationToken ct
	)
	{
		// Те же каналы, для которых одиночное включение провряет членство
		var candidates = destinations
			.Where(d => !d.IsActive || d.ChatStatus is ChatStatus.Left or ChatStatus.Banned)
			.ToList();

		if (candidates.Count == 0)
			return Result(0, 0, 0);

		var joinedChats = await chatService.TryGetJoinedChatsAsync(sessionId);
		if (!joinedChats.IsSuccess)
			throw new RepostDestinationsMembershipCheckException(DescribeFailure(joinedChats));

		var chats = joinedChats.Value!;
		List<Guid> toEnable = [];
		var notMember = 0;
		var noPermission = 0;

		foreach (var destination in candidates)
		{
			if (!chats.TryGetValue(TelegramChatId.ResolveRaw(destination.ChatId), out var chat))
			{
				notMember++;
				continue;
			}

			if (!chat.CanSendMessages || !chat.CanSendMedia)
			{
				noPermission++;
				continue;
			}

			toEnable.Add(destination.Id);
		}

		if (toEnable.Count > 0)
			await storage.ActivateAsync(toEnable, ct);

		return Result(toEnable.Count, notMember, noPermission);
	}

	private static SetRepostDestinationsActiveResponse Result(int updated, int notMember, int noPermission) => new()
	{
		UpdatedCount = updated,
		NotMemberCount = notMember,
		NoPermissionCount = noPermission
	};

	private static string DescribeFailure<T>(TelegramOperationResult<T> result) => result.Status switch
	{
		TelegramOperationStatus.FloodWait => "Telegram временно ограничил аккаунт, попробуйте позже",
		TelegramOperationStatus.SessionNotFound => "Telegram-сессия не авторизована или выключена",
		_ => result.ErrorMessage ?? result.Status.ToString()
	};
}
