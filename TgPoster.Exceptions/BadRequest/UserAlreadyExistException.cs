using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class UserAlreadyExistException() : DomainException("Пользователь уже существует. Используйте другой логин.");
