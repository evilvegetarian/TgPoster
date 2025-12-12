using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(
	Guid ScheduleId,
	int PageNumber,
	int PageSize,
	MessageSortBy SortBy,
	SortDirection SortDirection,
	string? SearchText,
	MessageStatus Status) : IRequest<PagedResponse<MessageResponse>>;

public enum MessageStatus
{
	/// <summary>
	/// Все
	/// </summary>
	All,

	/// <summary>
	/// Запланировано
	/// </summary>
	Planed,

	/// <summary>
	/// Не подтверждено
	/// </summary>
	NotApproved,

	/// <summary>
	/// Доставлено
	/// </summary>
	Delivered,

	/// <summary>
	/// Не доставлено
	/// </summary>
	NotDelivered
}

public enum MessageSortBy
{
	/// <summary>
	/// По дате создания
	/// </summary>
	CreatedAt,

	/// <summary>
	/// По дате отправки
	/// </summary>
	SentAt
}

public enum SortDirection
{
	/// <summary>
	/// По возрастанию
	/// </summary>
	Asc,

	/// <summary>
	/// По убыванию
	/// </summary>
	Desc
}