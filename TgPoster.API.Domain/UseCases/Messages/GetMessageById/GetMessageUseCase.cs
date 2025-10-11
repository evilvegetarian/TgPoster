using MediatR;
using Security.Interfaces;
using Shared;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

internal sealed class GetMessageUseCase(
    IGetMessageStorage storage,
    IIdentityProvider identity
) : IRequestHandler<GetMessageQuery, MessageResponse>
{
    public async Task<MessageResponse> Handle(GetMessageQuery request, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var message = await storage.GetMessagesAsync(request.Id, userId, ct);

        if (message is null)
        {
            throw new MessageNotFoundException(request.Id);
        }

        return new MessageResponse
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            CanApprove = true,
            NeedApprove = !message.IsVerified,
            Files = message.Files.Select(file => new FileResponse
            {
                Id = file.Id,
                FileType = file.ContentType.GetFileType()
            }).ToList()
        };
    }
}