namespace TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;

public interface IDeletePublishedMessageStorage
{
	/// <summary>
	/// Пост из расписания пользователя, чужой или несуществующий — null
	/// </summary>
	/// <param name="id"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<PublishedMessageInfo?> GetMessageAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	/// Мягко удалить пост из списка постов
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	Task DeleteMessageAsync(Guid id, CancellationToken ct);
}
