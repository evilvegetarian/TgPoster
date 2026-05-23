namespace TgPoster.Telegram.Models;

/// <summary>
///     Тип медиа в Telegram-сообщении
/// </summary>
public enum TelegramMediaType
{
	/// <summary>Фотография</summary>
	Photo,

	/// <summary>Видео (документ с DocumentAttributeVideo)</summary>
	Video,

	/// <summary>Другой документ (аудио, файл и т.п.)</summary>
	Document,

	/// <summary>Прочее (опросы, контакты, локация и т.п.)</summary>
	Other
}

/// <summary>
///     Тип фрагмента-сущности в тексте сообщения
/// </summary>
public enum TelegramMessageEntityType
{
	/// <summary>Простой URL в тексте (https://example.com)</summary>
	Url,

	/// <summary>Текстовая ссылка (URL хранится отдельно, в свойстве <see cref="TelegramMessageEntity.Url"/>)</summary>
	TextUrl,

	/// <summary>Упоминание пользователя через @username</summary>
	Mention,

	/// <summary>Хэштег</summary>
	Hashtag,

	/// <summary>Команда бота (/start и т.п.)</summary>
	BotCommand,

	/// <summary>Прочая сущность, не используемая Domain</summary>
	Other
}

/// <summary>
///     Сущность внутри текста сообщения (ссылка, упоминание, хэштег и т.п.)
/// </summary>
/// <param name="Offset">Смещение от начала текста (в UTF-16 кодовых единицах)</param>
/// <param name="Length">Длина фрагмента</param>
/// <param name="Type">Тип сущности</param>
/// <param name="Url">URL — заполнен только для <see cref="TelegramMessageEntityType.TextUrl"/></param>
public sealed record TelegramMessageEntity(int Offset, int Length, TelegramMessageEntityType Type, string? Url);

/// <summary>
///     Медиа-вложение в Telegram-сообщении.
///     <see cref="Source"/> — internal-деталь, Domain не должен её читать
/// </summary>
public sealed class TelegramMessageMedia
{
	/// <summary>Тип медиа</summary>
	public required TelegramMediaType Type { get; init; }

	/// <summary>MIME-тип контента (если применимо)</summary>
	public string? MimeType { get; init; }

	/// <summary>
	///     Опаковая ссылка на оригинальный TL-объект (Photo/Document), нужная для последующей загрузки.
	///     ВНИМАНИЕ: это internal-деталь библиотеки, Domain читать это поле не должен
	/// </summary>
	internal object? Source { get; init; }
}

/// <summary>
///     Сообщение в Telegram-канале/чате
/// </summary>
public sealed class TelegramMessage
{
	/// <summary>ID сообщения (уникален в пределах peer'а)</summary>
	public required int Id { get; init; }

	/// <summary>Дата отправки</summary>
	public required DateTime Date { get; init; }

	/// <summary>Текст сообщения (или подпись к медиа)</summary>
	public string? Text { get; init; }

	/// <summary>
	///     ID группы (альбома) — все сообщения с одинаковым GroupedId образуют альбом.
	///     null, если сообщение одиночное
	/// </summary>
	public long? GroupedId { get; init; }

	/// <summary>Медиа-вложение (если есть)</summary>
	public TelegramMessageMedia? Media { get; init; }

	/// <summary>Сущности внутри текста (ссылки, упоминания и т.п.)</summary>
	public IReadOnlyList<TelegramMessageEntity>? Entities { get; init; }
}
