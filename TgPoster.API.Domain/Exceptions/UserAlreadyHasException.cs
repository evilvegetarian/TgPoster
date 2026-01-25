namespace TgPoster.API.Domain.Exceptions;

public class UserAlreadyExistException() : DomainException("Пользователь уже существует. Используйте другой логин.");