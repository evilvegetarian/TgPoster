namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

public interface IApproveMessagesStorage
{
    Task ApproveMessage(List<Guid> messageIds, CancellationToken ct);
}