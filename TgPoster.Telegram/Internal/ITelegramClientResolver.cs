using Shared.Enums;
using WTelegram;

namespace TgPoster.Telegram.Internal;

/// <summary>
///     Internal-абстракция для разрешения <see cref="Client" /> по идентификатору сессии.
///     Использется только внутри библиотеки TgPoster.Telegram — Domain не должен напрямую получать Client
/// </summary>
internal interface ITelegramClientResolver
{
	Task<Client?> GetClientAsync(Guid sessionId, CancellationToken ct = default);
	Task<Client?> GetClientAsync(TelegramSessionPurpose purpose, CancellationToken ct = default);
}