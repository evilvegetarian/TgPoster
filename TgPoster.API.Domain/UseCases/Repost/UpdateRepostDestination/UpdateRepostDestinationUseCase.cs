using MediatR;
using Shared.Enums;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

internal sealed class UpdateRepostDestinationUseCase(
	IUpdateRepostDestinationStorage storage,
	ITelegramChatService chatService)
	: IRequestHandler<UpdateRepostDestinationCommand>
{
	public async Task Handle(UpdateRepostDestinationCommand request, CancellationToken ct)
	{
		var destination = await storage.GetDestinationAsync(request.Id, ct);
		if (destination == null)
			throw new RepostDestinationNotFoundException(request.Id);

		var chatStatus = destination.ChatStatus;

		// Направление отключают, когда аккаунт вышел из чата или его исключили, поэтому при включении
		// аккаунт возвращается в чат, иначе следующий репост снова отключит направление
		if (request.IsActive && NeedsRejoin(destination))
		{
			await RejoinAsync(destination);
			chatStatus = ChatStatus.Active;
		}

		await storage.UpdateDestinationAsync(
			request.Id,
			request.IsActive,
			chatStatus,
			request.DelayMinSeconds,
			request.DelayMaxSeconds,
			request.RepostEveryNth,
			request.SkipProbability,
			request.MaxRepostsPerDay,
			ct);
	}

	private static bool NeedsRejoin(RepostDestinationInfo destination) =>
		!destination.IsActive || destination.ChatStatus is ChatStatus.Left or ChatStatus.Banned;

	/// <summary>
	/// Проверяет, что аккаунт состоит в чате и может пересылать туда медиа. Если не состоит, вступает по username
	/// </summary>
	/// <param name="destination"></param>
	/// <returns></returns>
	private async Task RejoinAsync(RepostDestinationInfo destination)
	{
		var chat = await chatService.TryGetChatInfoAsync(
			destination.TelegramSessionId,
			destination.ChatId.ToString(),
			false);

		if (chat.Status == TelegramOperationStatus.UsernameNotFound)
		{
			if (string.IsNullOrWhiteSpace(destination.Username))
				throw new RepostDestinationRejoinException(
					"аккаунт не состоит в чате, а у чата нет username — вступить можно только по ссылке-приглашению");

			chat = await chatService.TryGetChatInfoAsync(destination.TelegramSessionId, destination.Username, true);
		}

		if (!chat.IsSuccess)
			throw new RepostDestinationRejoinException(DescribeFailure(chat));

		var info = chat.Value!;

		// Username мог перейит к другому чату: тогда вступили не туда, куда настроен репост
		if (info.Id != TelegramChatId.ResolveRaw(destination.ChatId))
			throw new RepostDestinationRejoinException($"username @{destination.Username} теперь у другого чата");

		// Репост пересылает и медиа, поэтому нужны оба права, как при добавлении канала
		if (!info.CanSendMessages)
			throw new TelegramChatNoWritePermissionException(info.Title);

		if (!info.CanSendMedia)
			throw new TelegramChatNoMediaPermissionException(info.Title);
	}

	private static string DescribeFailure(TelegramOperationResult<TelegramChatInfo> result) => result.Status switch
	{
		TelegramOperationStatus.ChannelBanned => "аккаунт заблокирован в чате",
		TelegramOperationStatus.UsernameNotFound => "чат не найден по username",
		TelegramOperationStatus.FloodWait or TelegramOperationStatus.SpamRestricted =>
			"Telegram временно ограничил аккаунт, попробуйте позже",
		TelegramOperationStatus.SessionNotFound => "Telegram-сессия не авторизована или выключена",
		_ => result.ErrorMessage ?? result.Status.ToString()
	};
}
