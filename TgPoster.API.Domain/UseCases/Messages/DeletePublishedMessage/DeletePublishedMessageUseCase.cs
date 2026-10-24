using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;

/// <summary>
/// Use case удаления опубликованного поста из канала: альбом и отдельное текстовое сообщение целиком
/// </summary>
internal sealed class DeletePublishedMessageUseCase(
	IDeletePublishedMessageStorage storage,
	IIdentityProvider identity,
	TelegramTokenService tokenService,
	IPublishedMessageService publishedMessageService,
	TimeProvider timeProvider
) : IRequestHandler<DeletePublishedMessageCommand>
{
	public async Task Handle(DeletePublishedMessageCommand request, CancellationToken ct)
	{
		var message = await storage.GetMessageAsync(request.Id, identity.Current.UserId, ct);
		if (message is null)
		{
			throw new MessageNotFoundException(request.Id);
		}

		if (!message.IsPublished)
		{
			throw new MessageNotPublishedException(request.Id);
		}

		if (message.TelegramMessageIds is not { Length: > 0 } ids)
		{
			throw new PublishedMessageIdsUnknownException();
		}

		if (!PublishedMessageDeleteWindow.IsOpen(message.TimePosting, timeProvider.GetUtcNow()))
		{
			throw new PublishedMessageDeleteExpiredException();
		}

		var (token, _) = await tokenService.GetTokenByScheduleIdAsync(message.ScheduleId, ct);
		var result = await publishedMessageService.DeleteAsync(token, message.ChannelId, ids, ct);
		if (!result.IsSuccess)
		{
			throw new PublishedMessageDeleteFailedException(result.Error);
		}

		await storage.DeleteMessageAsync(request.Id, ct);
	}
}
