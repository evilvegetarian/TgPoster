using Shared.Enums;

namespace Shared.Telegram;

/// <summary>
///     Результат обновления информации о канале/чате с учётом статуса доступа.
/// </summary>
public sealed class TelegramChannelRefreshResult
{
	public string? Title { get; init; }
	public string? Username { get; init; }
	public int? MemberCount { get; init; }
	public ChatType ChatType { get; init; }
	public ChatStatus ChatStatus { get; init; }
	public byte[]? AvatarThumbnail { get; init; }
}
