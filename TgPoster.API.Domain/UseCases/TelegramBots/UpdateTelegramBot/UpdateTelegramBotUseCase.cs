using MediatR;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;

internal sealed class UpdateTelegramBotUseCase(TelegramTokenService service, IUpdateTelegramBotStorage storage)
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
			var bot = new TelegramBotClient(token);
			var botInfo = await bot.GetMe(ct);
			nameBot = botInfo.Username;
		}

		await storage.UpdateTelegramBotAsync(request.Id, nameBot, request.IsActive, ct);
	}
}