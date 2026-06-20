using MediatR;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.Exceptions;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;

internal sealed class UpdateTelegramBotUseCase(
	TelegramTokenService service,
	IUpdateTelegramBotStorage storage,
	TelegramBotManager botManager)
	: IRequestHandler<UpdateTelegramBotCommand>
{
	public async Task Handle(UpdateTelegramBotCommand request, CancellationToken ct)
	{
		var (token, _) = await service.GetTokenByTelegramIdAsync(request.Id, ct);
		if (token is null)
		{
			throw new TelegramBotNotFoundException(request.Id);
		}

		var nameBot = request.Name;
		if (nameBot is null)
		{
			var bot = botManager.GetClient(token);
			var botInfo = await bot.GetMe(ct);
			nameBot = botInfo.Username ?? throw new InvalidOperationException("Имя бота не может быть null");
		}

		await storage.UpdateTelegramBotAsync(request.Id, nameBot, request.IsActive, ct);
	}
}