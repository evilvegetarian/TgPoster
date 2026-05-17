using System.Net;
using Microsoft.Extensions.Logging;

namespace Shared.Telegram;

/// <inheritdoc cref="ITelegramPublicLookupService"/>
internal sealed class TelegramPublicLookupService(
    IHttpClientFactory httpClientFactory,
    ILogger<TelegramPublicLookupService> logger) : ITelegramPublicLookupService
{
    private const string BaseUrl = "https://t.me/";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    // t.me отдаёт более полную страницу для «браузерного» UA, поэтому имитируем Chrome.
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public async Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupAsync(
        string usernameOrUrl,
        CancellationToken ct = default)
    {
        var parseResult = TelegramChatService.ParseInput(usernameOrUrl ?? string.Empty);
        if (parseResult.Type != ChatInputType.Username)
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                TelegramOperationStatus.UsernameNotFound,
                $"Не удалось распознать username во входе: '{usernameOrUrl}'");
        }

        var username = parseResult.Value;
        var url = BaseUrl + username;

        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Таймаут при запросе t.me для username {Username}", username);
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                TelegramOperationStatus.Timeout, "Таймаут HTTP-запроса к t.me");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ошибка HTTP-запроса t.me для username {Username}", username);
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                TelegramOperationStatus.UnknownError, ex.Message);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
                {
                    Username = username,
                    Type = TelegramEntityType.NotFound
                });
            }

            if ((int)response.StatusCode >= 500)
            {
                logger.LogWarning(
                    "Неожиданный HTTP-код {StatusCode} от t.me для username {Username}",
                    (int)response.StatusCode, username);
                return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                    TelegramOperationStatus.UnknownError,
                    $"HTTP {(int)response.StatusCode}");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Неожиданный HTTP-код {StatusCode} от t.me для username {Username}",
                    (int)response.StatusCode, username);
                return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                    TelegramOperationStatus.UnknownError,
                    $"HTTP {(int)response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync(ct);
            var info = TelegramPublicLookupHtmlParser.Parse(username, html);

            logger.LogDebug(
                "t.me lookup для {Username}: {Type}",
                username, info.Type);

            return TelegramOperationResult<TelegramPublicEntityInfo>.Success(info);
        }
    }
}
