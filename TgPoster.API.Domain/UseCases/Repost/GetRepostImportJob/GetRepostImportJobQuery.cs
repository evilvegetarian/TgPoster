using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;

/// <summary>
///     Получение состояния задания на массовое добавление целевых каналов
/// </summary>
/// <param name="JobId">Id задания</param>
public sealed record GetRepostImportJobQuery(Guid JobId) : IRequest<RepostImportJobResponse>;
