namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

/// <summary>
/// Статус доставки поста в канал
/// </summary>
public enum MessageDeliveryStatus
{
	/// <summary>
	/// Запланирован
	/// </summary>
	Planned,

	/// <summary>
	/// Отправляется
	/// </summary>
	Sending,

	/// <summary>
	/// Отправлен
	/// </summary>
	Sent,

	/// <summary>
	/// Отправка завершилась ошибкой
	/// </summary>
	Failed,

	/// <summary>
	/// Отменён
	/// </summary>
	Cancelled
}
