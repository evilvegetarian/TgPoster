namespace TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;

/// <summary>
/// Срок, в который бот может удалить опубликованный пост из канала
/// </summary>
internal static class PublishedMessageDeleteWindow
{
	/// <summary>
	/// Bot API даёт удалить сообщение в течение 48 часов, запас в 5 минут покрывает задрежку отправки
	/// </summary>
	private static readonly TimeSpan Duration = TimeSpan.FromHours(48) - TimeSpan.FromMinutes(5);

	/// <summary>
	/// До какого момента пост ещё можно удалить из канала
	/// </summary>
	/// <param name="timePosting"></param>
	/// <returns></returns>
	public static DateTimeOffset GetDeadline(DateTimeOffset timePosting) => timePosting + Duration;

	/// <summary>
	/// Можно ли ещё удалить пост из канала
	/// </summary>
	/// <param name="timePosting"></param>
	/// <param name="now"></param>
	/// <returns></returns>
	public static bool IsOpen(DateTimeOffset timePosting, DateTimeOffset now) => now < GetDeadline(timePosting);
}
