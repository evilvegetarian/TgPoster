using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.Services;
using TgPoster.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.Domain.UseCases.Messages.GetMessageById;

internal sealed class GetMessageUseCase(
    IGetMessageStorage storage,
    IIdentityProvider identity,
    TelegramTokenService tokenService,
    FileService fileService
) : IRequestHandler<GetMessageQuery, MessageResponse>
{
    public async Task<MessageResponse> Handle(GetMessageQuery request, CancellationToken cancellationToken)
    {
        var userId = identity.Current.UserId;
        var message = await storage.GetMessage(request.Id, userId, cancellationToken);

        if (message == null)
            throw new MessageNotFoundException();

        var token = await tokenService.GetDecryptToken(message.ScheduleId, cancellationToken);

        var bot = new TelegramBotClient(token);

        var filesCacheInfos = await fileService.ProcessFilesAsync(bot, message.Files, cancellationToken);
        return new MessageResponse
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            Files = filesCacheInfos.Select(file => new FileResponse
            {
                Id = file.Id,
                FileType = file.FileType,
                FileCacheId = file.FileCacheId,
                PreviewCacheIds = file.PreviewCacheIds
            }).ToList()
        };
    }
}