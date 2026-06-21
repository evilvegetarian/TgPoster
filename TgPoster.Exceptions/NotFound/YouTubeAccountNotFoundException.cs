using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class YouTubeAccountNotFoundException(Guid? id = null)
	: NotFoundException($"YouTube аккаунт с идентификатором {id} не найден.");