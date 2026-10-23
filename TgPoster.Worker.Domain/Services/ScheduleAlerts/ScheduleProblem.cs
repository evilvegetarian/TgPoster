namespace TgPoster.Worker.Domain.Services.ScheduleAlerts;

/// <summary>
/// Проблема с расписанием, о которой нужно оповестить владельца
/// </summary>
public enum ScheduleProblem
{
	/// <summary>
	/// Пост не удалось отправить в канал
	/// </summary>
	PostFailed,

	/// <summary>
	/// Бот потерял доступ к каналу: его выкинули, лишили прав или канал удалён
	/// </summary>
	BotLostAccess,

	/// <summary>
	/// Отправка постов прервалась перезапуском воркера
	/// </summary>
	PostsInterrupted,

	/// <summary>
	/// Запланированные посты скоро закончатся
	/// </summary>
	QueueRunningLow,

	/// <summary>
	/// Запланированных постов не осталось
	/// </summary>
	QueueEmpty,

	/// <summary>
	/// Не удалось спарсить источник постов
	/// </summary>
	ParsingFailed
}
