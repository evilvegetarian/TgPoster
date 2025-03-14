using MediatR;
using Microsoft.Extensions.Options;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

internal sealed class ListMessageUseCase(
    IListMessageStorage storage,
    TelegramOptions options,
    ICryptoAES aes,
    FileService fileService
) : IRequestHandler<ListMessageQuery, List<MessageResponse>>
{
    public async Task<List<MessageResponse>> Handle(ListMessageQuery request, CancellationToken cancellationToken)
    {
        if (!await storage.ExistSchedule(request.ScheduleId, cancellationToken))
            throw new ScheduleNotFoundException();

        var encryptedToken = await storage.GetApiToken(request.ScheduleId, cancellationToken);
        if (encryptedToken == null)
            throw new TelegramNotFoundException();

        var token = aes.Decrypt(options.SecretKey, encryptedToken);

        var bot = new TelegramBotClient(token);
        var message = await storage.GetMessagesAsync(request.ScheduleId, cancellationToken);
        var messages = new List<MessageResponse>();
        foreach (var m in message)
        {
            var filesCacheInfos = await fileService.ProcessFilesAsync(bot, m.Files, cancellationToken);
            messages.Add(new MessageResponse
            {
                Id = m.Id,
                TextMessage = m.TextMessage,
                ScheduleId = m.ScheduleId,
                TimePosting = m.TimePosting,
                Files = filesCacheInfos.Select(file => new FileResponse
                {
                    Id = file.Id,
                    FileType = file.FileType,
                    FileCacheId = file.FileCacheId,
                    PreviewCacheIds = file.PreviewCacheIds
                }).ToList()
            });
        }

        return messages;
    }
}