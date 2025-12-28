using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

public interface IEditMessageStorage
{
	Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct);
	Task UpdateMessageAsync(EditMessageCommand message, List<MediaFileResult> files, CancellationToken ct);
}