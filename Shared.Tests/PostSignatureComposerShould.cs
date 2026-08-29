using Shared.Utilities;
using Shouldly;

namespace Shared.Tests;

/// <summary>
///     Тесты для класса PostSignatureComposer
/// </summary>
public sealed class PostSignatureComposerShould
{
	private const string Signature = "<a href=\"https://t.me/channel\">Подписаться</a>";

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ReturnTextUnchanged_WhenSignatureIsEmpty(string? signature)
	{
		var result = PostSignatureComposer.Compose("Текст поста", signature, TelegramLimits.CaptionLength);

		result.ShouldBe("Текст поста");
	}

	[Fact]
	public void ReturnEmptyString_WhenTextAndSignatureAreEmpty()
	{
		var result = PostSignatureComposer.Compose(null, null, TelegramLimits.CaptionLength);

		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ReturnOnlySignature_WhenTextIsEmpty(string? text)
	{
		var result = PostSignatureComposer.Compose(text, Signature, TelegramLimits.CaptionLength);

		result.ShouldBe(Signature);
	}

	[Fact]
	public void AppendSignatureBelowText_WhenEverythingFits()
	{
		var result = PostSignatureComposer.Compose("Текст поста", Signature, TelegramLimits.CaptionLength);

		result.ShouldBe("Текст поста\n\n" + Signature);
	}

	[Fact]
	public void TrimTrailingWhitespaceOfText_WhenSignatureAppended()
	{
		var result = PostSignatureComposer.Compose("Текст поста   \n\n", Signature, TelegramLimits.CaptionLength);

		result.ShouldBe("Текст поста\n\n" + Signature);
	}

	[Fact]
	public void TruncateText_WhenCombinedExceedsLimit()
	{
		var result = PostSignatureComposer.Compose(new string('a', 100), "SIGN", 20);

		result.ShouldBe(new string('a', 13) + "…\n\nSIGN");
		result.Length.ShouldBe(20);
	}

	[Fact]
	public void KeepSignatureIntact_WhenTextIsTruncated()
	{
		var result = PostSignatureComposer.Compose(new string('a', 5000), Signature, TelegramLimits.MessageLength);

		result.Length.ShouldBeLessThanOrEqualTo(TelegramLimits.MessageLength);
		result.ShouldEndWith(Signature);
	}

	[Fact]
	public void ReturnOnlySignature_WhenSignatureFillsWholeLimit()
	{
		var result = PostSignatureComposer.Compose(new string('a', 100), "SIGN", 6);

		result.ShouldBe("SIGN");
	}

	[Fact]
	public void DropDanglingTag_WhenTruncationFallsInsideTag()
	{
		var result = PostSignatureComposer.Compose("12345<b>x</b>", "S", 11);

		result.ShouldBe("12345…\n\nS");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void TreatEmptyMarkupAsValid(string? html)
	{
		var result = PostSignatureComposer.IsValidTelegramHtml(html);

		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData("Подписывайтесь на канал")]
	[InlineData("<a href=\"https://t.me/channel\">Подписаться</a>")]
	[InlineData("<b>Жирный</b> и <i>курсив</i>")]
	[InlineData("<b><i>Вложенные</i></b>")]
	[InlineData("<tg-spoiler>Спойлер</tg-spoiler>")]
	[InlineData("<blockquote>Цитата</blockquote>")]
	public void AcceptSupportedMarkup(string html)
	{
		var result = PostSignatureComposer.IsValidTelegramHtml(html);

		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData("<div>Не поддерживается</div>")]
	[InlineData("<script>alert(1)</script>")]
	[InlineData("<br/>")]
	public void RejectUnsupportedTag(string html)
	{
		var result = PostSignatureComposer.IsValidTelegramHtml(html);

		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData("<b>Не закрыт")]
	[InlineData("Не открыт</b>")]
	[InlineData("<b><i>Перепутан порядок</b></i>")]
	public void RejectUnbalancedMarkup(string html)
	{
		var result = PostSignatureComposer.IsValidTelegramHtml(html);

		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData("Цена < 100 рублей")]
	[InlineData("<a href=\"https://t.me/channel\">Подписаться</a> < тут")]
	public void RejectStrayAngleBracket(string html)
	{
		var result = PostSignatureComposer.IsValidTelegramHtml(html);

		result.ShouldBeFalse();
	}
}
