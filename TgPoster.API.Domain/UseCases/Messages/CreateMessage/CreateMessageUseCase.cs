using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.Exceptions;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

internal sealed class CreateMessageUseCase(
	ICreateMessageStorage storage,
	IIdentityProvider identityProvider,
	ITelegramService telegramService,
	TelegramTokenService tokenService,
	TelegramBotManager botManager
) : IRequestHandler<CreateMessageCommand, CreateMessageResponse>
{
	public async Task<CreateMessageResponse> Handle(CreateMessageCommand request, CancellationToken ct)
	{
		var userId = identityProvider.Current.UserId;
		if (!await storage.ExistScheduleAsync(userId, request.ScheduleId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var (token, chatId) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);

		var bot = botManager.GetClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.Files, chatId, ct);

		var id = await storage.CreateMessagesAsync(request.ScheduleId, request.Text, request.TimePosting, files, ct);
		return new CreateMessageResponse
		{
			Id = id
		};
	}
}