using MediatR;
using Security.Cryptography;
using Security.IdentityServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

internal sealed class UpdateScheduleUseCase(
	IUpdateScheduleStorage storage,
	IIdentityProvider provider,
	TelegramOptions options,
	ICryptoAES aes
) : IRequestHandler<UpdateScheduleCommand>
{
	public async Task Handle(UpdateScheduleCommand request, CancellationToken ct)
	{
		var userId = provider.Current.UserId;

		if (request.TelegramBotId is not null)
		{
			var encryptedToken = await storage.GetApiTokenAsync(request.TelegramBotId.Value, userId, ct);
			if (encryptedToken is null)
				throw new TelegramBotNotFoundException(request.TelegramBotId.Value);

			var channelName = await storage.GetChannelNameAsync(request.Id, userId, ct);
			if (channelName is null)
				throw new ScheduleNotFoundException(request.Id);

			var token = aes.Decrypt(options.SecretKey, encryptedToken);
			var bot = new TelegramBotClient(token);
			var botMember = await bot.GetChatMember(channelName, bot.BotId, ct);
			if (botMember is not ChatMemberAdministrator { CanPostMessages: true })
				throw new TelegramBotNotPermission();
		}

		await storage.UpdateScheduleAsync(request.Id, userId, request.Name, request.YouTubeAccountId, request.TelegramBotId, ct);
	}
}
