using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

internal sealed class CreateMessageUseCase(
	ICreateMessageStorage storage,
	IIdentityProvider identityProvider,
	TelegramService telegramService,
	TelegramTokenService tokenService
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

		var bot = new TelegramBotClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.Files, chatId, ct);

		var id = await storage.CreateMessagesAsync(request.ScheduleId, request.Text, request.TimePosting, files, ct);
		return new CreateMessageResponse
		{
			Id = id
		};
	}
}