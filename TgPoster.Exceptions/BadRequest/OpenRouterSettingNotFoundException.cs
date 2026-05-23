using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class OpenRouterSettingNotFoundException() : DomainException("Настройки Open Router не существует");
