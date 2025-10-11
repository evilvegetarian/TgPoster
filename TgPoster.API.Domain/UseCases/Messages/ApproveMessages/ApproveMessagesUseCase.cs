using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

internal class ApproveMessagesUseCase(IApproveMessagesStorage storage) : IRequestHandler<ApproveMessagesCommand>
{
    public Task Handle(ApproveMessagesCommand request, CancellationToken ct)
    {
        return storage.ApproveMessage(request.MessageIds, ct);
    }
}