using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

/// <summary>
///     Запрос настроек LLM-классификатора каналов
/// </summary>
public sealed record GetClassifierSettingsQuery : IRequest<ClassifierSettingsResponse>;
