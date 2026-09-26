using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Аккаунт соцсети неактивен
/// </summary>
public sealed class SocialAccountInactiveException()
	: DomainException("Аккаунт соцсети удалён или требует переподключения");
