
using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;


public record ApproveMessagesCommand(List<Guid> messageIds):IRequest;

internal class ApproveMessagesUseCase : IRequestHandler<ApproveMessagesCommand>
{
    public async Task Handle(ApproveMessagesCommand request, CancellationToken ct)
    {
        
    }
}