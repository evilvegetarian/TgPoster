namespace TgPoster.API.Models;

/// <summary>
///     Статус сообщения
/// </summary>
public enum MessageStatus
{
	/// <summary>
	///     Все сообщения
	/// </summary>
	All,

	/// <summary>
	///     Запланированное сообщение
	/// </summary>
	Planed,

	/// <summary>
	///     Не подтвержденное сообщение
	/// </summary>
	NotApproved,

	/// <summary>
	///     Доставленное сообщение
	/// </summary>
	Delivered,

	/// <summary>
	///     Не доставленное сообщение
	/// </summary>
	NotDelivered
}

/// <summary>
///     Поля для сортировки сообщений
/// </summary>
public enum MessageSortBy
{
	/// <summary>
	///     По дате создания
	/// </summary>
	CreatedAt,

	/// <summary>
	///     По дате отправки
	/// </summary>
	SentAt
}

/// <summary>
///     Направление сортировки
/// </summary>
public enum SortDirection
{
	/// <summary>
	///     По возрастанию
	/// </summary>
	Asc,

	/// <summary>
	///     По убыванию
	/// </summary>
	Desc
}

/// <summary>
///     Расширения для работы с enum
/// </summary>
public static class EnumExtensions
{
	/// <summary>
	///     Получить название статуса сообщения
	/// </summary>
	public static string GetName(this MessageStatus status)
	{
		return status switch
		{
			MessageStatus.All => "Все",
			MessageStatus.Planed => "Запланировано",
			MessageStatus.NotApproved => "Не подтверждено",
			MessageStatus.Delivered => "Доставлено",
			MessageStatus.NotDelivered => "Не доставлено",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}

	/// <summary>
	///     Получить название поля сортировки
	/// </summary>
	public static string GetName(this MessageSortBy sortBy)
	{
		return sortBy switch
		{
			MessageSortBy.CreatedAt => "По дате создания",
			MessageSortBy.SentAt => "По дате отправления",
			_ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
		};
	}

	/// <summary>
	///     Получить название направления сортировки
	/// </summary>
	public static string GetName(this SortDirection sortDirection)
	{
		return sortDirection switch
		{
			SortDirection.Asc => "По возрастанию",
			SortDirection.Desc => "По убыванию",
			_ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
		};
	}
}