using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

public sealed class Message : BaseEntity
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Время поста
	/// </summary>
	public required DateTimeOffset TimePosting { get; set; }

	/// <summary>
	///     Текстовое сообщение
	/// </summary>
	public string? TextMessage { get; set; }

	/// <summary>
	///     Если в TextMessage больше 1024 символов, в нем не могут быть файлы,
	///     Если меньше или равно 1024 то, текст становится caption к первому файлу.
	/// </summary>
	public required bool IsTextMessage { get; set; }

	/// <summary>
	///     Статус сообщения
	/// </summary>
	public MessageStatus Status { get; set; }

	/// <summary>
	///     Сообщение верифицировано
	///     Дефолтно ставится true
	/// </summary>
	public bool IsVerified { get; set; }

	/// <summary>
	///     ID сообщения в Telegram после отправки.
	/// </summary>
	public int? TelegramMessageId { get; set; }

	/// <summary>
	///     Id настройки парсинга канала (null если сообщение создано вручную)
	/// </summary>
	public Guid? ChannelParsingSettingId { get; set; }

	#region Navigtion

	/// <summary>
	///     Расписание
	/// </summary>
	public Schedule Schedule { get; set; } = null!;

	/// <summary>
	///     Файлы сообщения
	/// </summary>
	public ICollection<MessageFile> MessageFiles { get; set; } = [];

	/// <summary>
	///     Настройка парсинга канала
	/// </summary>
	public ChannelParsingSetting? ChannelParsingSetting { get; set; }

	/// <summary>
	///     Журналы репостов этого сообщения
	/// </summary>
	public ICollection<RepostLog> RepostLogs { get; set; } = [];

	#endregion
}

public static class MessageExtenstion
{
	public static bool IsTextMessage(this string? text) => text?.Length >= 1024;
}