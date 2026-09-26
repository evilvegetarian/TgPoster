using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;

/// <summary>
///     Сохранить настройки LLM-классификатора каналов
/// </summary>
/// <param name="IsEnabled">Включён ли классификатор</param>
/// <param name="Model">Модель OpenRouter</param>
/// <param name="BatchSize">Сколько каналов классифицировать за один запуск</param>
/// <param name="IntervalMinutes">Как часто запускать классификатор, в минутах</param>
/// <param name="MessageSampleCount">Сколько последних постов канала брать в выборку</param>
/// <param name="PhotoCount">Сколько фото из постов отправлять в модель</param>
/// <param name="ReclassifyAfterDays">Через сколько дней классифицировать канал заново (null — никогда)</param>
/// <param name="Categories">Тематики</param>
/// <param name="SystemPrompt">Системный промпт с плейсхолдером списка тематик</param>
/// <param name="TelegramSessionId">Telegram-сессия классификатора (null — по назначению Classification)</param>
public sealed record UpdateClassifierSettingsCommand(
	bool IsEnabled,
	string Model,
	int BatchSize,
	int IntervalMinutes,
	int MessageSampleCount,
	int PhotoCount,
	int? ReclassifyAfterDays,
	IReadOnlyList<string> Categories,
	string SystemPrompt,
	Guid? TelegramSessionId) : IRequest;
