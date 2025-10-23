namespace TgPoster.API.Models;

public enum MessageStatus
{
	All,
	Planed,
	NotApproved,
	Delivered
}

// Для примера определим поля для сортировки
public enum MessageSortBy
{
	CreatedAt, // По дате создания
	SentAt // По дате отправки
}

public enum SortDirection
{
	Asc, // По возрастанию
	Desc // По убыванию
}

public static class EnumExtensions
{
	public static string GetName(this MessageStatus status)
	{
		return status switch
		{
			MessageStatus.All => "Все",
			MessageStatus.Planed => "Запланировано",
			MessageStatus.NotApproved => "Не подтверждено",
			MessageStatus.Delivered => "Доставлено",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}

	public static string GetName(this MessageSortBy sortBy)
	{
		return sortBy switch
		{
			MessageSortBy.CreatedAt => "По дате создания",
			MessageSortBy.SentAt => "По дате отправления",
			_ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
		};
	}

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