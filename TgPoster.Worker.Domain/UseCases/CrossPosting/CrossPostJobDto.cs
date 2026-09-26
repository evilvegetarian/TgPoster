using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     DTO задания на публикацию кросс-поста
/// </summary>
public sealed class CrossPostJobDto
{
	/// <summary>
	///     Id кросс-поста
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Id сообщения
	/// </summary>
	public required Guid MessageId { get; init; }

	/// <summary>
	///     Id аккаунта соцсети
	/// </summary>
	public required Guid SocialAccountId { get; init; }

	/// <summary>
	///     Площадка
	/// </summary>
	public required SocialPlatform Platform { get; init; }

	/// <summary>
	///     Количество предыдущих попыток
	/// </summary>
	public required int Attempts { get; init; }

	/// <summary>
	///     Текст сообщения
	/// </summary>
	public required string? TextMessage { get; init; }

	/// <summary>
	///     Формат кросс-поста (уже вычислен)
	/// </summary>
	public required CrossPostFormat Format { get; init; }

	/// <summary>
	///     Куда вести ссылку
	/// </summary>
	public required CrossPostLinkTarget LinkTarget { get; init; }

	/// <summary>
	///     Произвольная ссылка
	/// </summary>
	public required string? CustomLink { get; init; }

	/// <summary>
	///     Варианты призыва к действию
	/// </summary>
	public required string? CallToAction { get; init; }

	/// <summary>
	///     Прикладывать медиа
	/// </summary>
	public required bool IncludeMedia { get; init; }

	/// <summary>
	///     Имя канала
	/// </summary>
	public required string ChannelName { get; init; }

	/// <summary>
	///     Id сообщения в Telegram
	/// </summary>
	public required int? TelegramMessageId { get; init; }

	/// <summary>
	///     Зашифрованный токен бота
	/// </summary>
	public required string BotTokenEncrypted { get; init; }

	/// <summary>
	///     Имя аккаунта соцсети
	/// </summary>
	public required string AccountName { get; init; }

	/// <summary>
	///     Внешний id пользователя соцсети
	/// </summary>
	public required string AccountExternalUserId { get; init; }

	/// <summary>
	///     Зашифрованный секрет аккаунта
	/// </summary>
	public required string AccountSecretEncrypted { get; init; }

	/// <summary>
	///     Файлы сообщения
	/// </summary>
	public required List<CrossPostFileDto> Files { get; init; }
}
