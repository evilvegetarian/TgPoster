using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

public interface IEditMessageStorage
{
	/// <summary>
	/// Пост из расписания пользователя, чужой или несуществующий — null
	/// </summary>
	/// <param name="messageId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<EditMessageInfo?> GetMessageAsync(Guid messageId, Guid userId, CancellationToken ct);

	Task UpdateMessageAsync(EditMessageCommand message, List<MediaFileResult> files, CancellationToken ct);
}
