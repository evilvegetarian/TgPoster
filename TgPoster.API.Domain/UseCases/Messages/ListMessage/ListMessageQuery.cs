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
    All, // Все
    Planed, // Запланировано
    NotApproved, // Не подтверждено
    Delivered // Доставлено
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