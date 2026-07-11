namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

/// <summary>
///     Информация о настройках репоста для добавления целевого канала:
///     сессия и общие настройки, копируемые на новый канал
/// </summary>
/// <param name="TelegramSessionId">Id телеграм сессии для выполнения репостов</param>
/// <param name="DefaultDelayMinSeconds">Общая минимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultDelayMaxSeconds">Общая максимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultRepostEveryNth">Общая настройка "репостить каждое N-е сообщение"</param>
/// <param name="DefaultSkipProbability">Общая вероятность пропуска репоста (0-100%)</param>
/// <param name="DefaultMaxRepostsPerDay">Общий лимит репостов в день (null = без лимита)</param>
public sealed record RepostSettingsInfo(
	Guid TelegramSessionId,
	int DefaultDelayMinSeconds,
	int DefaultDelayMaxSeconds,
	int DefaultRepostEveryNth,
	int DefaultSkipProbability,
	int? DefaultMaxRepostsPerDay);
