using System.Collections.Concurrent;
using System.Net;
using Telegram.Bot;

namespace Shared.Telegram;

/// <summary>
/// Менеджер для кеширования и переиспользования TelegramBotClient экземпляров
/// </summary>
/// <param name="proxy">
/// Прокси для запросов к api.telegram.org. Тот же <see cref="IWebProxy"/>, что и для парсинга t.me.
/// Если не зарегистрирован — запросы идут напрямую
/// </param>
public sealed class TelegramBotManager(IWebProxy? proxy = null) : IDisposable
{
    private readonly ConcurrentDictionary<string, TelegramBotClient> clients = [];

    /// <summary>
    /// Получает или создает TelegramBotClient для указанного токена
    /// </summary>
    /// <param name="token">Токен бота Telegram</param>
    /// <returns>Закэшированный или новый экземпляр <see cref="TelegramBotClient"/></returns>
    public TelegramBotClient GetClient(string token)
    {
        return clients.GetOrAdd(token, t =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                Proxy = proxy,
                UseProxy = proxy is not null
            };
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            return new TelegramBotClient(t, http);
        });
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
