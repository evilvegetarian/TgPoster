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

        var page = await FetchTmePageAsync(url, username, ct);
        if (!page.IsSuccess)
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(page.Status, page.ErrorMessage);
        }

        if (page.NotFound)
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
            {
                Username = username,
                Type = TelegramEntityType.NotFound
            });
        }

        var info = TelegramPublicLookupHtmlParser.Parse(username, page.Html!);

        return TelegramOperationResult<TelegramPublicEntityInfo>.Success(info);
    }

    public async Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupInviteAsync(
        string inviteHash,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteHash))
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(
                TelegramOperationStatus.UsernameNotFound,
                "Пустой инвайт-хеш");
        }

        var hash = inviteHash.Trim();
        var url = BaseUrl + "+" + hash;

        var page = await FetchTmePageAsync(url, hash, ct);
        if (!page.IsSuccess)
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Failed(page.Status, page.ErrorMessage);
        }

        if (page.NotFound)
        {
            return TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
            {
                Username = null,
                Type = TelegramEntityType.NotFound
            });
        }

        var info = TelegramPublicLookupHtmlParser.ParseInvite(page.Html!);

        return TelegramOperationResult<TelegramPublicEntityInfo>.Success(info);
    }

    private async Task<TmePageFetchResult> FetchTmePageAsync(string url, string subject, CancellationToken ct)
    {
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
            logger.LogWarning("Таймаут при запросе t.me для {Subject}", subject);
            return TmePageFetchResult.Fail(TelegramOperationStatus.Timeout, "Таймаут HTTP-запроса к t.me");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ошибка HTTP-запроса t.me для {Subject}", subject);
            return TmePageFetchResult.Fail(TelegramOperationStatus.UnknownError, ex.Message);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return TmePageFetchResult.OkNotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Неожиданный HTTP-код {StatusCode} от t.me для {Subject}",
                    (int)response.StatusCode, subject);
                return TmePageFetchResult.Fail(
                    TelegramOperationStatus.UnknownError,
                    $"HTTP {(int)response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync(ct);
            return TmePageFetchResult.Ok(html);
        }
    }

    private readonly record struct TmePageFetchResult(
        bool IsSuccess,
        bool NotFound,
        string? Html,
        TelegramOperationStatus Status,
        string? ErrorMessage)
    {
        public static TmePageFetchResult Ok(string html) =>
            new(true, false, html, TelegramOperationStatus.Success, null);

        public static TmePageFetchResult OkNotFound() =>
            new(true, true, null, TelegramOperationStatus.Success, null);

        public static TmePageFetchResult Fail(TelegramOperationStatus status, string? message) =>
            new(false, false, null, status, message);
    }
}
