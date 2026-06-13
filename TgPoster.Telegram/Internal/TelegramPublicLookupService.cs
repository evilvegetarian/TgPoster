using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Configuration;
using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Internal;

/// <inheritdoc cref="ITelegramPublicLookupService"/>
internal sealed class TelegramPublicLookupService(
	IHttpClientFactory httpClientFactory,
	IOptions<TelegramPublicLookupOptions> options,
	ILogger<TelegramPublicLookupService> logger) : ITelegramPublicLookupService
{
	private const string BaseUrl = "https://t.me/";

	private readonly TelegramPublicLookupOptions settings = options.Value;

	// t.me отдаёт более полную страницу для «браузерного» UA, поэтому имитируем разные браузеры
	private static readonly string[] UserAgents =
	[
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
		+ "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
		+ "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
		"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 "
		+ "(KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:130.0) Gecko/20100101 Firefox/130.0",
		"Mozilla/5.0 (Macintosh; Intel Mac OS X 14.6; rv:131.0) Gecko/20100101 Firefox/131.0",
		"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 "
		+ "(KHTML, like Gecko) Version/17.6 Safari/605.1.15",
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
		+ "(KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0",
		"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
		+ "(KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36"
	];

	public async Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupAsync(
		string usernameOrUrl,
		CancellationToken ct = default
	)
	{ 
		var parseResult = TelegramChatService.ParseInput(usernameOrUrl);
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
		CancellationToken ct = default
	)
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
		var client = httpClientFactory.CreateClient(DependencyInjection.PublicLookupClient);
		var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
		var maxRetries = Math.Max(0, settings.MaxRetries);

		for (var attempt = 0;; attempt++)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.UserAgent.ParseAdd(UserAgents[Random.Shared.Next(UserAgents.Length)]);
			request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(timeout);

			try
			{
				using var response = await client.SendAsync(
					request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token);

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					return TmePageFetchResult.OkNotFound();
				}

				if (response.IsSuccessStatusCode)
				{
					var html = await response.Content.ReadAsStringAsync(timeoutCts.Token);
					return TmePageFetchResult.Ok(html);
				}

				if (IsTransientStatus(response.StatusCode) && attempt < maxRetries)
				{
					await DelayBeforeRetryAsync(attempt, ct);
					continue;
				}

				logger.LogWarning(
					"Неожиданный HTTP-код {StatusCode} от t.me для {Subject}",
					(int)response.StatusCode, subject);
				return TmePageFetchResult.Fail(
					TelegramOperationStatus.UnknownError,
					$"HTTP {(int)response.StatusCode}");
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				if (attempt < maxRetries)
				{
					await DelayBeforeRetryAsync(attempt, ct);
					continue;
				}

				logger.LogWarning("Таймаут при запросе t.me для {Subject}", subject);
				return TmePageFetchResult.Fail(TelegramOperationStatus.Timeout, "Таймаут HTTP-запроса к t.me");
			}
			catch (HttpRequestException ex)
			{
				if (attempt < maxRetries)
				{
					await DelayBeforeRetryAsync(attempt, ct);
					continue;
				}

				logger.LogError(ex, "Ошибка HTTP-запроса t.me для {Subject}", subject);
				return TmePageFetchResult.Fail(TelegramOperationStatus.UnknownError, ex.Message);
			}
		}
	}

	private static bool IsTransientStatus(HttpStatusCode status) =>
		(int)status >= 500 || status == HttpStatusCode.TooManyRequests;

	private Task DelayBeforeRetryAsync(int attempt, CancellationToken ct)
	{
		var backoff = settings.RetryBaseDelayMs * (1 << attempt);
		var jitter = Random.Shared.Next(0, settings.RetryBaseDelayMs + 1);
		return Task.Delay(TimeSpan.FromMilliseconds(backoff + jitter), ct);
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