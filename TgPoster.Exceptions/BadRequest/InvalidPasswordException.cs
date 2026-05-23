using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class InvalidPasswordException() : DomainException("Неверный пароль.");
