namespace TgPoster.Storage.Data.Entities;

public sealed class Schedule : BaseEntity
{
	/// <summary>
	///     Наименование расписания.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	///     Id пользователя
	/// </summary>
	public required Guid UserId { get; set; }

	/// <summary>
	///     Id телеграм бота.
	/// </summary>
	public required Guid TelegramBotId { get; set; }

	/// <summary>
	///     Канал на который будет отправляться сообщения
	/// </summary>
	public required long ChannelId { get; set; }

	/// <summary>
	///     Название  канала на который будет отправляться сообщения
	/// </summary>
	public required string ChannelName { get; set; }

	/// <summary>
	///     Обозначает активность канала
	/// </summary>
	public required bool IsActive { get; set; }

	/// <summary>
	///     Общая подпись, приклеиваемая снизу к каждому посту расписания.
	///     Поддерживает HTML-разметку Telegram
	/// </summary>
	public string? SignatureFooter { get; set; }

	/// <summary>
	///     Признак того, что подпись добавляется к постам.
	///     Позволяет выключить подпись, не стирая её текст
	/// </summary>
	public bool SignatureEnabled { get; set; }

	/// <summary>
	///     Канал на который будет отправляться сообщения
	/// </summary>
	public Guid? YouTubeAccountId { get; set; }


	#region Навигация

	/// <summary>
	///     Телеграм бот.
	/// </summary>
	public TelegramBot TelegramBot { get; set; } = null!;

	/// <summary>
	///     Пользователь.
	/// </summary>
	//TODO: Вынести отдельно в Many-to-Many в будущем 
	public User User { get; set; } = null!;

	/// <summary>
	///     Дни постинга.
	/// </summary>
	public ICollection<Day> Days { get; set; } = [];

	/// <summary>
	///     Сообщение этого расписания
	/// </summary>
	public ICollection<Message> Messages { get; set; } = [];

	/// <summary>
	///     Настройки парсинга каналов для этого расписания
	/// </summary>
	public ICollection<ChannelParsingSetting> Parameters { get; set; } = [];

	/// <summary>
	///     Настройки промптов для этого расписания
	/// </summary>
	public PromptSetting? PromptSetting { get; set; }

	/// <summary>
	///     Настройки подключения к OpenRouter для этого расписания
	/// </summary>
	public OpenRouterSetting? OpenRouterSetting { get; set; }

	/// <summary>
	///     Настройки подключения к OpenRouter для этого расписания
	/// </summary>
	public YouTubeAccount? YouTubeAccount { get; set; }

	/// <summary>
	///     Настройки репоста для этого расписания
	/// </summary>
	public List<RepostSettings> RepostSettings { get; set; } = [];

	/// <summary>
	///     Связки кросс-постинга для этого расписания
	/// </summary>
	public ICollection<CrossPostTarget> CrossPostTargets { get; set; } = [];

	#endregion
}