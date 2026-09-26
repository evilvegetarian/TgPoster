using Shared.CrossPosting;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для класса TelegramHtmlToText
/// </summary>
public sealed class TelegramHtmlToTextShould
{
	[Fact]
	public void ReturnEmptyString_WhenHtmlIsNull()
	{
		var result = TelegramHtmlToText.Convert(null);

		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("   ", "")]
	[InlineData("<b>Жирный</b> и <i>курсив</i>", "Жирный и курсив")]
	[InlineData("<a href=\"https://example.com\">сайт</a> тут", "сайт тут")]
	[InlineData("Секрет: <tg-spoiler>ответ</tg-spoiler>", "Секрет: [спойлер]")]
	[InlineData("Секрет: <span class=\"tg-spoiler\">ответ</span>", "Секрет: [спойлер]")]
	[InlineData("a &lt; b &amp;&amp; c &gt; d &quot;q&quot;", "a < b && c > d \"q\"")]
	[InlineData("Строка1\r\nСтрока2", "Строка1\nСтрока2")]
	[InlineData("Абзац1\n\n\n\nАбзац2", "Абзац1\n\nАбзац2")]
	[InlineData("Текст  \nещё", "Текст\nещё")]
	[InlineData("<tg-emoji emoji-id=\"5368324170671202286\">👍</tg-emoji> Класс", "👍 Класс")]
	[InlineData("<blockquote>Цитата</blockquote>\nПосле", "Цитата\nПосле")]
	[InlineData("Строка<br>вторая", "Строка\nвторая")]
	[InlineData("&lt;b&gt;не тег&lt;/b&gt;", "<b>не тег</b>")]
	[InlineData("<pre><code class=\"language-cs\">var x = 1;</code></pre>", "var x = 1;")]
	public void ConvertHtmlToPlainText(string html, string expected)
	{
		var result = TelegramHtmlToText.Convert(html);

		result.ShouldBe(expected);
	}
}
