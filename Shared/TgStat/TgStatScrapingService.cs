using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Shared.TgStat.Models;

namespace Shared.TgStat;

/// <summary>
///     Реализация скрейпинга tgstat.ru через Playwright headless Chromium.
/// </summary>
internal sealed partial class TgStatScrapingService(ILogger<TgStatScrapingService> logger) : ITgStatScrapingService
{
	private const string BaseUrl = "https://tgstat.ru";
	private const int MinDelayMs = 2000;
	private const int MaxDelayMs = 5000;
	private const int PageLoadTimeoutMs = 30000;

	/// <inheritdoc />
	public async Task<TgStatChannelDetailDto?> ScrapeChannelDetailAsync(string channelUrl, CancellationToken ct)
	{
		logger.LogDebug("Парсим детали канала: {Url}", channelUrl);

		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = true
		});

		var context = await browser.NewContextAsync(new BrowserNewContextOptions
		{
			Locale = "ru-RU",
			UserAgent =
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
		});

		var page = await context.NewPageAsync();

		try
		{
			var url = channelUrl.StartsWith("http") ? channelUrl : $"{BaseUrl}{channelUrl}";

			await page.GotoAsync(url, new PageGotoOptions
			{
				WaitUntil = WaitUntilState.NetworkIdle,
				Timeout = PageLoadTimeoutMs
			});

			await RandomDelayAsync(ct);

			// Название канала
			var titleElement = await page.QuerySelectorAsync("h1, .channel-name, [class*='peer-title']");
			var title = titleElement is not null ? (await titleElement.InnerTextAsync()).Trim() : null;

			if (string.IsNullOrEmpty(title))
				return null;

			// Описание
			var descriptionElement = await page.QuerySelectorAsync(
				".channel-description, [class*='peer-description'], [class*='about']");
			var description = descriptionElement is not null
				? (await descriptionElement.InnerTextAsync()).Trim()
				: null;

			// Аватарка
			var avatarElement = await page.QuerySelectorAsync(
				".channel-avatar img, [class*='peer-avatar'] img, .peer-photo img");
			var avatarUrl = avatarElement is not null
				? await avatarElement.GetAttributeAsync("src")
				: null;

			// Username — берём из кнопки-ссылки вида <a href="https://t.me/username" class="btn btn-outline-info ...">
			var usernameElement = await page.QuerySelectorAsync(
				"a.btn-outline-info[href*='t.me/']");
			string? username = null;
			if (usernameElement is not null)
			{
				var href = await usernameElement.GetAttributeAsync("href");
				if (href is not null && href.Contains("t.me/"))
					username = href.Split("t.me/").Last().TrimEnd('/');
			}

			var tUrlElement= await page.QuerySelectorAsync("a.btn-outline-info[href*='t.me/']");
			string? tUrl = null;
			if (tUrlElement is not null)
			{
				var href = await tUrlElement.GetAttributeAsync("href");
				if (href is not null && href.Contains("t.me/"))
					tUrl = href;
			}
			// Подписчики
			var participantsCount = await ExtractStatValueAsync(page,
				"[class*='subscribers'], [class*='participants'], [class*='members']");
			return new TgStatChannelDetailDto
			{
				TgUrl =tUrl ,
				Title = title,
				Username = username,
				Description = description,
				AvatarUrl = avatarUrl,
				ParticipantsCount = participantsCount,
				PeerType = await DetectPeerTypeAsync(page)
			};
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			logger.LogError(ex, "Ошибка при парсинге деталей канала {Url}", channelUrl);
			return null;
		}
	}

	private static async Task<int> ExtractStatValueAsync(IPage page, string selector)
	{
		var element = await page.QuerySelectorAsync(selector);
		if (element is null) return 0;

		var text = await element.InnerTextAsync();
		return ParseFormattedNumber(text);
	}

	private static async Task<string?> DetectPeerTypeAsync(IPage page)
	{
		var content = await page.ContentAsync();
		if (content.Contains("группа", StringComparison.OrdinalIgnoreCase) ||
		    content.Contains("group", StringComparison.OrdinalIgnoreCase) ||
		    content.Contains("chat", StringComparison.OrdinalIgnoreCase))
			return "chat";

		return "channel";
	}

	private static int ParseFormattedNumber(string text)
	{
		var cleaned = text.Trim();

		var kMatch = KiloRegex().Match(cleaned);
		if (kMatch.Success &&
		    double.TryParse(kMatch.Groups[1].Value.Replace(",", "."), CultureInfo.InvariantCulture, out var kVal))
			return (int)(kVal * 1000);

		var mMatch = MegaRegex().Match(cleaned);
		if (mMatch.Success &&
		    double.TryParse(mMatch.Groups[1].Value.Replace(",", "."), CultureInfo.InvariantCulture, out var mVal))
			return (int)(mVal * 1_000_000);

		var digitsOnly = NonDigitRegex().Replace(cleaned, "");
		return int.TryParse(digitsOnly, out var result) ? result : 0;
	}

	private static async Task RandomDelayAsync(CancellationToken ct)
	{
		var delay = Random.Shared.Next(MinDelayMs, MaxDelayMs);
		await Task.Delay(delay, ct);
	}

	[GeneratedRegex(@"([\d,\.]+)\s*[kкК]", RegexOptions.IgnoreCase)]
	private static partial Regex KiloRegex();

	[GeneratedRegex(@"([\d,\.]+)\s*[mмМ]", RegexOptions.IgnoreCase)]
	private static partial Regex MegaRegex();

	[GeneratedRegex(@"[^\d]")]
	private static partial Regex NonDigitRegex();
}
