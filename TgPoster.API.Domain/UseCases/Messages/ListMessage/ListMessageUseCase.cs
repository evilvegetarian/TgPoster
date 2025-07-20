using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

internal sealed class ListMessageUseCase(
    IListMessageStorage storage,
    TelegramOptions options,
    ICryptoAES aes,
    FileService fileService,
    IIdentityProvider provider
) : IRequestHandler<ListMessageQuery, List<MessageResponse>>
{
    public async Task<List<MessageResponse>> Handle(ListMessageQuery request, CancellationToken ct)
    {
        if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ct))
        {
            throw new ScheduleNotFoundException();
        }

        var encryptedToken = await storage.GetApiTokenAsync(request.ScheduleId, ct);
        if (encryptedToken == null)
        {
            throw new TelegramNotFoundException();
        }

        var token = aes.Decrypt(options.SecretKey, encryptedToken);

        var bot = new TelegramBotClient(token);
        var message = await storage.GetMessagesAsync(request.ScheduleId, ct);
        var messages = new List<MessageResponse>();
        foreach (var m in message)
        {
            var filesCacheInfos = await fileService.ProcessFilesAsync(bot, m.Files, ct);
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