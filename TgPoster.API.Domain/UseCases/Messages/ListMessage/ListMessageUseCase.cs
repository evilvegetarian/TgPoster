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
) : IRequestHandler<ListMessageQuery, PagedResponse<MessageResponse>>
{
    public async Task<PagedResponse<MessageResponse>> Handle(ListMessageQuery request, CancellationToken ct)
    {
        if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ct))
            throw new ScheduleNotFoundException(request.ScheduleId);

        var encryptedToken = await storage.GetApiTokenAsync(request.ScheduleId, ct);
        if (encryptedToken == null)
            throw new TelegramNotFoundException();

        var token = aes.Decrypt(options.SecretKey, encryptedToken);
        var bot = new TelegramBotClient(token);

        var pagedMessages =
            await storage.GetMessagesAsync(request.ScheduleId, request.PageNumber, request.PageSize, ct);

        var messageResponses = new List<MessageResponse>();
        foreach (var m in pagedMessages.Items)
        {
            var filesCacheInfos = await fileService.ProcessFilesAsync(bot, m.Files, ct);
            messageResponses.Add(new MessageResponse
            {
                Id = m.Id,
                TextMessage = m.TextMessage,
                ScheduleId = m.ScheduleId,
                TimePosting = m.TimePosting,
                NeedApprove = !m.IsVerified,
                CanApprove = true,
                Files = filesCacheInfos.Select(file => new FileResponse
                {
                    Id = file.Id,
                    FileType = file.FileType,
                    FileCacheId = file.FileCacheId,
                    PreviewCacheIds = file.PreviewCacheIds
                }).ToList()
            });
        }

        return new PagedResponse<MessageResponse>(
            messageResponses,
            pagedMessages.TotalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}