using MassTransit;
using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;

internal sealed class StartChannelCleanupUseCase(
	IStartChannelCleanupStorage storage,
	ITelegramMessageService messageService,
	IBus bus,
	IIdentityProvider identity)
	: IRequestHandler<StartChannelCleanupCommand, ChannelCleanupJobResponse>
{
	public async Task<ChannelCleanupJobResponse> Handle(StartChannelCleanupCommand request, CancellationToken ct)
	{
		if (request.ChatIds.Count == 0)
		{
			throw new ChannelCleanupException("Не выбрано ни одного канала");
		}

		var exists = await storage.SessionExistsAsync(request.SessionId, identity.Current.UserId, ct);
		if (!exists)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		var activeJobId = await storage.GetActiveJobIdAsync(request.SessionId, ct);
		if (activeJobId != null)
		{
			throw new ChannelCleanupException("Для этого аккаунта очистка уже идёт, дождитесь её завершения");
		}

		// Список из предпросмотра не доверяем как есть: пересобираем кандидатов заново,
		// чтобы не выйти из канала, который за это время начал использоваться
		var chats = await messageService.GetJoinedChatsAsync(request.SessionId, ct);
		if (!chats.IsSuccess)
		{
			throw new ChannelCleanupException(ChannelCleanupCandidates.DescribeLoadFailure(chats));
		}

		var used = await storage.GetUsedChatsAsync(request.SessionId, ct);
		var requested = request.ChatIds.ToHashSet();

		var items = ChannelCleanupCandidates.Select(chats.Value!, used).Candidates
			.Where(x => requested.Contains(x.Id))
			.Select(x => new ChannelCleanupItemDraft(
				x.Id,
				x.AccessHash,
				x.Peer.Type == TelegramPeerType.Chat,
				ChannelCleanupCandidates.ToChatType(x),
				x.Title,
				x.Username))
			.ToList();

		if (items.Count == 0)
		{
			throw new ChannelCleanupException("Выходить не из чего: список устарел, обновите предпросмотр");
		}

		var jobId = await storage.CreateJobAsync(request.SessionId, items, ct);
		await bus.Publish(new CleanupChannelsContract { JobId = jobId }, ct);

		return ChannelCleanupJobResponse.Create(
			jobId,
			ChannelCleanupStatus.Pending,
			null,
			null,
			items
				.Select(x => new ChannelCleanupItemResponse
				{
					ChatId = x.ChatId,
					Title = x.Title,
					Username = x.Username,
					ChatType = x.ChatType,
					Outcome = ChannelCleanupOutcome.Pending
				})
				.ToList());
	}
}
