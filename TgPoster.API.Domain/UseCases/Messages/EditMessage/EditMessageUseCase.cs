using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

internal class EditMessageUseCase(
	IEditMessageStorage storage,
	IIdentityProvider provider,
	TelegramTokenService tokenService,
	ITelegramService telegramService,
	TelegramBotManager botManager)
	: IRequestHandler<EditMessageCommand>
{
	public async Task Handle(EditMessageCommand request, CancellationToken ct)
	{
		var userId = provider.Current.UserId;
		if (!await storage.ExistMessageAsync(request.Id, userId, ct))
		{
			throw new MessageNotFoundException(request.Id);
		}

		var (token, chatId) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);

		var bot = botManager.GetClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.NewFiles, chatId, ct);

		await storage.UpdateMessageAsync(request, files, ct);
	}
}