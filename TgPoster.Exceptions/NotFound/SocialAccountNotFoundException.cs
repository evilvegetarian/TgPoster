using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

/// <summary>
///     Аккаунт соцсети не найден
/// </summary>
public sealed class SocialAccountNotFoundException(Guid id)
	: NotFoundException($"Аккаунт соцсети {id} не найден");
