using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class InvalidYouTubeOAuthStateException()
	: DomainException("Сессия подключения YouTube недействительна или истекла. Начните подключение заново");
