using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Enum;

public enum ParsingStatus
{
	/// <summary>
	///     Только что созданое задание на парсинг
	/// </summary>
	New = 0,

	/// <summary>
	///     В обработке, означает что сейчас уже парсится
	/// </summary>
	InHandle = 1,

	/// <summary>
	///     Пользователь отменил этот парсинг
	/// </summary>
	Canceled = 2,

	/// <summary>
	///     Ожидает новой итерации парсинга
	/// </summary>
	Waiting = 3,

	/// <summary>
	///     Работа полностью закончена
	/// </summary>
	Finished = 4,

	/// <summary>
	///     Ошибка
	/// </summary>
	Failed = 100
}

public static class ParsingStatusExtensions
{
	public static string GetStatus(this ParsingStatus status)
	{
		return status switch
		{
			ParsingStatus.New => "Новый запрос парсинга",
			ParsingStatus.InHandle => "В процессе парсинга",
			ParsingStatus.Canceled => "Отменено пользователем",
			ParsingStatus.Waiting => "Ожидает",
			ParsingStatus.Finished => "Закончен парсинг",
			ParsingStatus.Failed => "Ошибка",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}

	public static bool IsActive(this ParsingStatus status) =>
		status != ParsingStatus.Finished && status != ParsingStatus.Canceled;

	public static IQueryable<ChannelParsingSetting> IsActiveAndDontUse(
		this IQueryable<ChannelParsingSetting> queryable
	)
	{
		return queryable.Where(x => x.Status != ParsingStatus.Finished
		                            && x.Status != ParsingStatus.Canceled
		                            && x.Status != ParsingStatus.InHandle);
	}
}