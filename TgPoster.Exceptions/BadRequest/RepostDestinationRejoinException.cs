using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Направление репоста не включено: аккаунт не смог вернуться в целевой чат
/// </summary>
/// <param name="reason"></param>
public sealed class RepostDestinationRejoinException(string reason)
	: DomainException($"Направление не включено: {reason}");
