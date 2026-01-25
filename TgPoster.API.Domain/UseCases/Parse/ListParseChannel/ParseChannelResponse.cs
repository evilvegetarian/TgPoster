namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

public sealed record ParseChannelResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required bool DeleteText { get; init; }
	public required bool DeleteMedia { get; init; }
	public required string[] AvoidWords { get; init; } = [];
	public required bool NeedVerifiedPosts { get; init; }
	public DateTime? DateFrom { get; init; }
	public DateTime? DateTo { get; init; }

	/// <summary>
	///     Тестовый статус парсинга
	/// </summary>
	public required string Status { get; init; }

	/// <summary>
	///     Работает ли парсинг
	/// </summary>
	public required bool IsActive { get; init; }

	public required string Channel { get; init; }
	public DateTime? LastParseDate { get; init; }

	/// <summary>
	///     Telegram сессия для парсинга канала.
	/// </summary>
	public required Guid TelegramSessionId { get; init; }
}

public sealed record ParseChannelListResponse
{
	public required List<ParseChannelResponse> Items { get; init; }
}