using Shared.SharedException;

namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterSettingNotFoundException() : SharedException("Настройки Open Router не существует");