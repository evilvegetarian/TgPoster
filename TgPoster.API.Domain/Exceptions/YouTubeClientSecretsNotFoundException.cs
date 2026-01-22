namespace TgPoster.API.Domain.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда не найдены учётные данные клиента YouTube (ClientId или ClientSecret).
/// </summary>
public sealed class YouTubeClientSecretsNotFoundException()
	: DomainException("Учётные данные YouTube (ClientId или ClientSecret) не найдены в конфигурации");
