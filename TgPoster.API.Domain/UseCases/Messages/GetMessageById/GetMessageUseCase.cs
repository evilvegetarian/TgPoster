using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

internal sealed class GetMessageUseCase(
    IGetMessageStorage storage,
    IIdentityProvider identity,
    TelegramTokenService tokenService,
    FileService fileService
) : IRequestHandler<GetMessageQuery, MessageResponse>
{
    public async Task<MessageResponse> Handle(GetMessageQuery request, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var message = await storage.GetMessagesAsync(request.Id, userId, ct);

        if (message == null)
        {
            throw new MessageNotFoundException(request.Id);
        }

        var token = await tokenService.GetTokenByScheduleIdAsync(message.ScheduleId, ct);

        var bot = new TelegramBotClient(token);

        var filesCacheInfos = await fileService.ProcessFilesAsync(bot, message.Files, ct);
        return new MessageResponse
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            CanApprove = true,
            NeedApprove = !message.IsVerified,
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