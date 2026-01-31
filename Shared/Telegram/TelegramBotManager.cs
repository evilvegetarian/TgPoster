using System.Collections.Concurrent;
using Telegram.Bot;

namespace Shared.Telegram;

/// <summary>
/// Менеджер для кеширования и переиспользования TelegramBotClient экземпляров
/// </summary>
public sealed class TelegramBotManager : IDisposable
{
    private readonly ConcurrentDictionary<string, TelegramBotClient> clients = [];

    /// <summary>
    /// Получает или создает TelegramBotClient для указанного токена
    /// </summary>
    public TelegramBotClient GetClient(string token)
    {
        return clients.GetOrAdd(token, t => new TelegramBotClient(t));
    }

    /// <summary>
    /// Удаляет клиента из кеша (например, при удалении бота из БД)
    /// </summary>
    public bool RemoveClient(string token)
    {
        return clients.TryRemove(token, out _);
    }

    public void Dispose()
    {
        clients.Clear();
    }
}
