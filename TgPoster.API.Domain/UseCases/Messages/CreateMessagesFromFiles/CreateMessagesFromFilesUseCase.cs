using MediatR;
using Shared.Services;
using Shared.Telegram;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

internal sealed class CreateMessagesFromFilesUseCase(
	ICreateMessagesFromFilesUseCaseStorage storage,
	ITelegramService telegramService,
	TimePostingService timePostingService,
	TelegramTokenService tokenService,
	TelegramBotManager botManager
) : IRequestHandler<CreateMessagesFromFilesCommand>
{
	public async Task Handle(CreateMessagesFromFilesCommand request, CancellationToken ct)
	{
		var (token, chatId) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);

		var bot = botManager.GetClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.Files, chatId, ct);
		var existTime = await storage.GetExistMessageTimePostingAsync(request.ScheduleId, ct);
		var scheduleTime = await storage.GetScheduleTimeAsync(request.ScheduleId, ct);

		var postingTime = timePostingService.GetTimeForPosting(request.Files.Count, scheduleTime, existTime);

		await storage.CreateMessagesAsync(request.ScheduleId, files, postingTime, ct);
	}
}