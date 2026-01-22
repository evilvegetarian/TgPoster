namespace TgPoster.API.Domain.Exceptions;

public class YouTubeAccountNotFoundException(Guid? id = null)
	: NotFoundException($"YouTube аккаунт с идентификатором {id} не найден.");