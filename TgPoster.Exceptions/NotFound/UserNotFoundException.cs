using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class UserNotFoundException() : NotFoundException("Пользователь не найден.");
