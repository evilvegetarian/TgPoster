using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

/// <summary>
/// Текущее состояние целевого канала: по нему решается, нужно ли при включении вступать в чат заново.
/// ChatStatus — только членство в чате, бан аккаунта приходит отдельно из реестра банов
/// </summary>
/// <param name="TelegramSessionId"></param>
/// <param name="ChatId"></param>
/// <param name="Username"></param>
/// <param name="IsActive"></param>
/// <param name="ChatStatus"></param>
/// <param name="IsAccountBanned"></param>
public sealed record RepostDestinationInfo(
	Guid TelegramSessionId,
	long ChatId,
	string? Username,
	bool IsActive,
	ChatStatus ChatStatus,
	bool IsAccountBanned = false);
