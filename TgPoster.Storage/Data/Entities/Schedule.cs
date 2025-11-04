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
	
	public Guid? PromptSettingId { get; set; }
	public Guid? OpenRouterSettingId { get; set; }

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
	/// Сообщение этого расписания
	/// </summary>
	public ICollection<Message> Messages { get; set; } = [];

	/// <summary>
	/// Настройки парсинга каналов для этого расписания
	/// </summary>
	public ICollection<ChannelParsingSetting> Parameters { get; set; } = [];

	/// <summary>
	/// Настройки промптов для этого расписания
	/// </summary>
	public PromptSetting? PromptSetting { get; set; }
	public OpenRouterSetting? OpenRouterSetting { get; set; }

	#endregion
}