using MediatR;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;

/// <summary>
/// Запрос журнала парсинга по источникам текущего пользователя, от свежих проходов к старым
/// </summary>
/// <param name="Page">Номер страницы</param>
/// <param name="PageSize">Размер страницы</param>
/// <param name="ParseChannelId">Только проходы этого источника</param>
/// <param name="Status">Только проходы с этим итогом</param>
/// <param name="From">Начало периода по времени прохода (включительно)</param>
/// <param name="To">Конец периода по времени прохода (включительно)</param>
public sealed record ListParseChannelRunsQuery(
	int Page,
	int PageSize,
	Guid? ParseChannelId,
	ParseChannelRunStatus? Status,
	DateTimeOffset? From,
	DateTimeOffset? To) : IRequest<PagedResponse<ParseChannelRunResponse>>;
