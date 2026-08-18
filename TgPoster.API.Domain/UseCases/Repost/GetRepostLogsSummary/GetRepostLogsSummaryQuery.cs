using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;

/// <summary>
///     Запрос сводки по журналу репостов за период.
/// </summary>
/// <param name="RepostSettingsId">Фильтр по настройкам репоста</param>
/// <param name="DestinationId">Фильтр по целевому каналу</param>
/// <param name="MessageId">Фильтр по конкретному сообщению</param>
/// <param name="From">Начало периода (включительно)</param>
/// <param name="To">Конец периода (включительно)</param>
public sealed record GetRepostLogsSummaryQuery(
	Guid? RepostSettingsId,
	Guid? DestinationId,
	Guid? MessageId,
	DateTimeOffset? From,
	DateTimeOffset? To) : IRequest<RepostLogsSummaryResponse>;
