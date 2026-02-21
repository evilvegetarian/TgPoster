using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

/// <summary>
///     Информация о канале для репоста.
/// </summary>
public sealed record RepostDestinationDto
{
	/// <summary>
	///     Id назначения репоста.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Id чата в Telegram.
	/// </summary>
	public required long ChatId { get; init; }

	/// <summary>
	///     Активность канала для репоста.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Название канала/чата.
	/// </summary>
	public string? Title { get; init; }

	/// <summary>
	///     Username (без @).
	/// </summary>
	public string? Username { get; init; }

	/// <summary>
	///     Количество подписчиков.
	/// </summary>
	public int? MemberCount { get; init; }

	/// <summary>
	///     Тип чата.
	/// </summary>
	public ChatType ChatType { get; init; }

	/// <summary>
	///     Статус доступа к чату.
	/// </summary>
	public ChatStatus ChatStatus { get; init; }

	/// <summary>
	///     Аватарка в формате base64 data URI.
	/// </summary>
	public string? AvatarBase64 { get; init; }

	/// <summary>
	///     Дата последнего обновления информации.
	/// </summary>
	public DateTimeOffset? InfoUpdatedAt { get; init; }

	/// <summary>
	///     Минимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMinSeconds { get; init; }

	/// <summary>
	///     Максимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMaxSeconds { get; init; }

	/// <summary>
	///     Репостить каждое N-е сообщение (1 = каждое).
	/// </summary>
	public int RepostEveryNth { get; init; }

	/// <summary>
	///     Вероятность пропуска репоста (0-100%).
	/// </summary>
	public int SkipProbability { get; init; }

	/// <summary>
	///     Максимальное количество репостов в день (null = без лимита).
	/// </summary>
	public int? MaxRepostsPerDay { get; init; }
}
