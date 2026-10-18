using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Каналы не включены: не удалось получить из Telegram список чатов, в которых состоит аккаунт
/// </summary>
/// <param name="reason"></param>
public sealed class RepostDestinationsMembershipCheckException(string reason)
	: DomainException($"Каналы не включены: не удалось проверить, в каких чатах состоит аккаунт — {reason}");
