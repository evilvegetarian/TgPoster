using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда настройки классификатора каналов не проходят проверку
/// </summary>
/// <param name="message">Что именно не так с настройками</param>
public sealed class InvalidClassifierSettingsException(string message) : DomainException(message);
