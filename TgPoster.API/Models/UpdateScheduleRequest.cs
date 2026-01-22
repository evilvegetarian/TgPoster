namespace TgPoster.API.Models;

/// <summary>
///     Запрос на обновление расписания
/// </summary>
public sealed class UpdateScheduleRequest
{
	/// <summary>
	///     Идентификатор YouTube аккаунта (опционально)
	/// </summary>
	public Guid? YouTubeAccountId { get; init; }
}