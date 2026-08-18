using MediatR;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;

/// <summary>
///     Запрос журнала репостов с фильтрацией и пагинацией.
/// </summary>
/// <param name="RepostSettingsId">Фильтр по настройкам репоста</param>
/// <param name="DestinationId">Фильтр по целевому каналу</param>
/// <param name="MessageId">Фильтр по конкретному сообщению</param>
/// <param name="Status">Фильтр по статусу репоста</param>
/// <param name="From">Начало периода (включительно)</param>
/// <param name="To">Конец периода (включительно)</param>
/// <param name="Page">Номер страницы</param>
/// <param name="PageSize">Размер страницы</param>
public sealed record ListRepostLogsQuery(
	Guid? RepostSettingsId,
	Guid? DestinationId,
	Guid? MessageId,
	RepostStatus? Status,
	DateTimeOffset? From,
	DateTimeOffset? To,
	int Page,
	int PageSize) : IRequest<PagedResponse<RepostLogDto>>;
