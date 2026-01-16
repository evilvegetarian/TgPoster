namespace TgPoster.API.Models;

/// <summary>
/// Запрос на подтверждение сообщений
/// </summary>
public sealed class ApproveMessagesRequest
{
	/// <summary>
	/// Список идентификаторов сообщений для подтверждения
	/// </summary>
	public List<Guid> MessagesIds { get; set; } = [];
}