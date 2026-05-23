using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace TgPoster.Telegram;

/// <summary>
///     Парсер HTML-страницы <c>https://t.me/&lt;username&gt;</c>.
///     Выделен отдельно, чтобы покрывать unit-тестами без HTTP.
/// </summary>
internal static partial class TelegramPublicLookupHtmlParser
{
    /// <summary>
    ///     Разбирает HTML публичной страницы t.me и возвращает информацию о сущности.
    /// </summary>
    /// <param name="username">Нормализованный username (без <c>@</c> и без <c>t.me/</c>), уже валидный.</param>
    /// <param name="html">HTML-ответ страницы <c>https://t.me/&lt;username&gt;</c>.</param>
    /// <returns>Описание сущности с заполненным <see cref="TelegramPublicEntityInfo.Type"/>.</returns>
    public static TelegramPublicEntityInfo Parse(string username, string html)
    {
        if (string.IsNullOrWhiteSpace(html) || !TitleRegex().IsMatch(html))
        {
            return new TelegramPublicEntityInfo
            {
                Username = username,
                Type = TelegramEntityType.NotFound
            };
        }

        var extraMatch = ExtraRegex().Match(html);
        var extraText = extraMatch.Success ? DecodeAndTrim(StripTags(extraMatch.Groups["body"].Value)) : null;

        var type = DetermineType(username, extraText);

        if (type is TelegramEntityType.User or TelegramEntityType.Bot)
        {
            return new TelegramPublicEntityInfo
            {
                Username = username,
                Type = type
            };
        }

        var titleMatch = TitleRegex().Match(html);
        var title = titleMatch.Success ? DecodeAndTrim(StripTags(titleMatch.Groups["body"].Value)) : null;

        var descriptionMatch = DescriptionRegex().Match(html);
        var description = descriptionMatch.Success
            ? DecodeAndTrim(StripTags(descriptionMatch.Groups["body"].Value))
            : null;

        var membersCount = extraText is not null ? ParseMembersCount(extraText) : null;

        var photoMatch = PhotoRegex().Match(html);
        var photoUrl = photoMatch.Success ? WebUtility.HtmlDecode(photoMatch.Groups["src"].Value).Trim() : null;

        return new TelegramPublicEntityInfo
        {
            Username = username,
            Type = type,
            Title = string.IsNullOrEmpty(title) ? null : title,
            Description = string.IsNullOrEmpty(description) ? null : description,
            MembersCount = membersCount,
            PhotoUrl = string.IsNullOrEmpty(photoUrl) ? null : photoUrl
        };
    }

    /// <summary>
    ///     Разбирает HTML страницы приглашения <c>https://t.me/+&lt;hash&gt;</c> и возвращает информацию о канале/группе.
    /// </summary>
    /// <param name="html">HTML-ответ страницы инвайта.</param>
    /// <returns>
    ///     Описание сущности. <see cref="TelegramPublicEntityInfo.Username"/> всегда <c>null</c> (на странице инвайта
    ///     публичного username нет). <see cref="TelegramPublicEntityInfo.Type"/> — <c>Channel</c>, <c>Group</c>
    ///     или <c>NotFound</c>, если HTML не похож на превью.
    /// </returns>
    public static TelegramPublicEntityInfo ParseInvite(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || !TitleRegex().IsMatch(html))
        {
            return new TelegramPublicEntityInfo
            {
                Username = null,
                Type = TelegramEntityType.NotFound
            };
        }

        var extraMatch = ExtraRegex().Match(html);
        var extraText = extraMatch.Success ? DecodeAndTrim(StripTags(extraMatch.Groups["body"].Value)) : null;

        var type = DetermineInviteType(extraText);

        var titleMatch = TitleRegex().Match(html);
        var title = titleMatch.Success ? DecodeAndTrim(StripTags(titleMatch.Groups["body"].Value)) : null;

        var descriptionMatch = DescriptionRegex().Match(html);
        var description = descriptionMatch.Success
            ? DecodeAndTrim(StripTags(descriptionMatch.Groups["body"].Value))
            : null;

        var membersCount = extraText is not null ? ParseMembersCount(extraText) : null;

        var photoMatch = PhotoRegex().Match(html);
        var photoUrl = photoMatch.Success ? WebUtility.HtmlDecode(photoMatch.Groups["src"].Value).Trim() : null;

        return new TelegramPublicEntityInfo
        {
            Username = null,
            Type = type,
            Title = string.IsNullOrEmpty(title) ? null : title,
            Description = string.IsNullOrEmpty(description) ? null : description,
            MembersCount = membersCount,
            PhotoUrl = string.IsNullOrEmpty(photoUrl) ? null : photoUrl
        };
    }

    private static TelegramEntityType DetermineType(string username, string? extraText)
    {
        if (extraText is not null)
        {
            if (extraText.Contains("subscriber", StringComparison.OrdinalIgnoreCase))
                return TelegramEntityType.Channel;

            if (extraText.Contains("member", StringComparison.OrdinalIgnoreCase))
                return TelegramEntityType.Group;
        }

        if (username.EndsWith("bot", StringComparison.OrdinalIgnoreCase))
            return TelegramEntityType.Bot;

        return TelegramEntityType.User;
    }

    private static TelegramEntityType DetermineInviteType(string? extraText)
    {
        if (extraText is not null)
        {
            if (extraText.Contains("subscriber", StringComparison.OrdinalIgnoreCase))
                return TelegramEntityType.Channel;

            if (extraText.Contains("member", StringComparison.OrdinalIgnoreCase))
                return TelegramEntityType.Group;
        }

        return TelegramEntityType.Channel;
    }

    private static long? ParseMembersCount(string extraText)
    {
        var match = MembersCountRegex().Match(extraText);
        if (!match.Success)
            return null;

        var digits = match.Groups["num"].Value
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(",", string.Empty)
            .Replace(".", string.Empty);

        return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string DecodeAndTrim(string raw)
    {
        var decoded = WebUtility.HtmlDecode(raw);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static string StripTags(string html) => TagRegex().Replace(html, string.Empty);

    [GeneratedRegex(
        @"<div\s+class=""tgme_page_title""[^>]*>(?<body>.*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(
        @"<div\s+class=""tgme_page_description""[^>]*>(?<body>.*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(
        @"<div\s+class=""tgme_page_extra""[^>]*>(?<body>.*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ExtraRegex();

    [GeneratedRegex(
        @"class=""tgme_page_photo_image""[^>]*>\s*(?:<img[^>]*src=""(?<src>[^""]+)""|)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PhotoRegex();

    [GeneratedRegex(
        @"(?<num>\d[\d\s ,\.]*)\s*(subscriber|member)",
        RegexOptions.IgnoreCase)]
    private static partial Regex MembersCountRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
