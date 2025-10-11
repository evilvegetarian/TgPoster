using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

internal class EditMessageUseCase(
    IEditMessageStorage storage,
    IIdentityProvider provider,
    TelegramTokenService tokenService,
    TelegramService telegramService)
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

        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.NewFiles, chatId, ct);

        await storage.UpdateMessageAsync(request, files, ct);
    }
}