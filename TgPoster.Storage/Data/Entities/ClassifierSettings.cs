namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Настройки LLM-классификатора обнаруженных каналов — одна запись на всю систему
/// </summary>
public sealed class ClassifierSettings : BaseEntity
{
	/// <summary>
	///     Фиксированный ID единственной записи: одновременное создание из API и воркера упрётся в первичный ключ,
	///     а не породит дубликат
	/// </summary>
	public static readonly Guid SingletonId = new("6c1a5f3e-0b7d-4c52-9a4e-3f2d8e1b7c90");

	/// <summary>Включён ли классификатор</summary>
	public bool IsEnabled { get; set; }

	/// <summary>Модель OpenRouter</summary>
	public required string Model { get; set; }

	/// <summary>Сколько каналов классифицировать за один запуск</summary>
	public int BatchSize { get; set; }

	/// <summary>Как часто запускать классификатор, в минутах</summary>
	public int IntervalMinutes { get; set; }

	/// <summary>Сколько последних постов канала брать в выборку</summary>
	public int MessageSampleCount { get; set; }

	/// <summary>Сколько фото из постов отправлять в модель (0 — не отправлять)</summary>
	public int PhotoCount { get; set; }

	/// <summary>Через сколько дней классифицировать канал заново (null — никогда)</summary>
	public int? ReclassifyAfterDays { get; set; }

	/// <summary>Тематики, из которых модель выбирает ровно одну</summary>
	public string[] Categories { get; set; } = [];

	/// <summary>Системный промпт с плейсхолдером списка тематик</summary>
	public required string SystemPrompt { get; set; }

	/// <summary>Telegram-сессия, через которую читаются посты (null — сессия с назначением Classification)</summary>
	public Guid? TelegramSessionId { get; set; }

	/// <summary>Telegram-сессия классификатора</summary>
	public TelegramSession? TelegramSession { get; set; }
}
