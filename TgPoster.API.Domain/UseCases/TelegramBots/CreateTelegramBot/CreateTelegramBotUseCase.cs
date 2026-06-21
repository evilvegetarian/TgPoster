using MediatR;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;

internal sealed class CreateTelegramBotUseCase(
	ICreateTelegramBotStorage storage,
	IIdentityProvider identity,
	ICryptoAES cryptoAes,
	TelegramOptions options,
	TelegramBotManager botManager
) : IRequestHandler<CreateTelegramBotCommand, CreateTelegramBotResponse>
{
	public async Task<CreateTelegramBotResponse> Handle(
		CreateTelegramBotCommand request,
		CancellationToken ct
	)
	{
		var bot = botManager.GetClient(request.ApiToken);
		var updates = await bot.GetUpdates(cancellationToken: ct);
		var chatId = updates
			             .Where(x => x.Message != null)
			             .Select(u => u.Message?.Chat.Id)
			             .FirstOrDefault()
		             ?? throw new ChatIdNotFoundException();

		var botInfo = await bot.GetMe(ct);
		var tokenEncrypted = cryptoAes.Encrypt(options.SecretKey, request.ApiToken);
		var id = await storage.CreateTelegramBotAsync(
			tokenEncrypted,
			chatId,
			identity.Current.UserId,
			botInfo.Username!,
			ct);

		return new CreateTelegramBotResponse
		{
			Id = id
		};
	}
}