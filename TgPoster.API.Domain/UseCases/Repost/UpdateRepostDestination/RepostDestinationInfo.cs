using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

/// <summary>
/// Текущее состояние целевого канала: по нему решается, нужно ли при включении вступать в чат заново
/// </summary>
/// <param name="TelegramSessionId"></param>
/// <param name="ChatId"></param>
/// <param name="Username"></param>
/// <param name="IsActive"></param>
/// <param name="ChatStatus"></param>
public sealed record RepostDestinationInfo(
	Guid TelegramSessionId,
	long ChatId,
	string? Username,
	bool IsActive,
	ChatStatus ChatStatus);
