using Shared.CrossPosting;
using Shared.Enums;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для CrossPostComposer
/// </summary>
public sealed class CrossPostComposerShould
{
	private static readonly PlatformTextProfile P40 = new(40, TextLengthMode.Utf16, true, false, false);

	[Theory]
	[InlineData("Короткий пост", "t.me/c/1", "Далее:", new[] { "Короткий пост\n\nДалее:\nt.me/c/1" }, false, false)]
	[InlineData("Сегодня мы расскажем о новой функции сервиса", "t.me/c/1", "Далее:", new[] { "Сегодня мы расскажем о…\n\nДалее:\nt.me/c/1" }, true, false)]
	[InlineData("Большое обновление приложения уже доступно", "t.me/c/1", "Далее:", new[] { "Большое обновление…\n\nДалее:\nt.me/c/1" }, true, false)]
	[InlineData("Итоги недели, главное, коротко и по делу сегодня", "t.me/c/1", "Далее:", new[] { "Итоги недели, главное…\n\nДалее:\nt.me/c/1" }, true, false)]
	[InlineData("Сегодня мы расскажем о новой функции сервиса", null, "Подписывайтесь!", new[] { "Сегодня мы расскажем о…\n\nПодписывайтесь!" }, true, false)]
	[InlineData("", "t.me/c/1", "Далее:", new[] { "Далее:\nt.me/c/1" }, false, false)]
	public void ComposeTeaser(string text, string? linkText, string callToAction, string[] expectedParts, bool truncated, bool fellBack)
	{
		var result = CrossPostComposer.Compose(text, CrossPostFormat.Teaser, linkText, callToAction, P40);

		result.Parts.ShouldBe(expectedParts);
		result.Truncated.ShouldBe(truncated);
		result.FellBackToTeaser.ShouldBe(fellBack);
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeAnnouncement()
	{
		var result = CrossPostComposer.Compose(
			"Любой длинный текст поста",
			CrossPostFormat.Announcement,
			"t.me/c/1",
			"Новый пост 👇",
			P40);

		result.Parts.ShouldBe(new[] { "Новый пост 👇\nt.me/c/1" });
		result.Truncated.ShouldBeFalse();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, P40);
	}

	[Theory]
	[InlineData("Короткий пост", "t.me/c", "Ещё:", new[] { "Короткий пост\n\nЕщё:\nt.me/c" }, false, false)]
	public void ComposeFull_Short(string text, string? linkText, string callToAction, string[] expectedParts, bool truncated, bool fellBack)
	{
		var result = CrossPostComposer.Compose(text, CrossPostFormat.Full, linkText, callToAction, P40);

		result.Parts.ShouldBe(expectedParts);
		result.Truncated.ShouldBe(truncated);
		result.FellBackToTeaser.ShouldBe(fellBack);
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_WithParagraphs_FooterSeparate()
	{
		var text = "Первый абзац текста поста.\n\nВторой абзац текста поста.\n\nТретий.";

		var result = CrossPostComposer.Compose(text, CrossPostFormat.Full, "t.me/c", "Ещё:", P40);

		result.Parts.ShouldBe(new[]
		{
			"Первый абзац текста поста.",
			"Второй абзац текста поста.\n\nТретий.",
			"Ещё:\nt.me/c"
		});
		result.Truncated.ShouldBeFalse();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_WithParagraphs_FooterAttached()
	{
		var text = "Первый абзац текста поста.\n\nВторой абзац.";

		var result = CrossPostComposer.Compose(text, CrossPostFormat.Full, "t.me/c", "Ещё:", P40);

		result.Parts.ShouldBe(new[]
		{
			"Первый абзац текста поста.",
			"Второй абзац.\n\nЕщё:\nt.me/c"
		});
		result.Truncated.ShouldBeFalse();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_WithSentences()
	{
		var text = "Первое предложение тут. Второе предложение тут. Третье.";

		var result = CrossPostComposer.Compose(text, CrossPostFormat.Full, "t.me/c", "Ещё:", P40);

		result.Parts.ShouldBe(new[]
		{
			"Первое предложение тут.",
			"Второе предложение тут. Третье.",
			"Ещё:\nt.me/c"
		});
		result.Truncated.ShouldBeFalse();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_FallsBackToTeaser_WhenTooManyParts()
	{
		var text = string.Join("\n\n", Enumerable.Repeat("Абзац с достаточно длинным текстом.", 10));

		var result = CrossPostComposer.Compose(text, CrossPostFormat.Full, "t.me/c", "Ещё:", P40);

		result.Parts.ShouldBe(new[] { "Абзац с достаточно длинным…\n\nЕщё:\nt.me/c" });
		result.Truncated.ShouldBeTrue();
		result.FellBackToTeaser.ShouldBeTrue();
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_WithoutChainSupport_FallsBackToTeaser()
	{
		var profile = new PlatformTextProfile(40, TextLengthMode.Utf16, false, false, false);

		var result = CrossPostComposer.Compose(
			"Сегодня мы расскажем о новой функции сервиса",
			CrossPostFormat.Full,
			"t.me/c/1",
			"Далее:",
			profile);

		result.Parts.ShouldBe(new[] { "Сегодня мы расскажем о…\n\nДалее:\nt.me/c/1" });
		result.Truncated.ShouldBeTrue();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, profile);
	}

	[Fact]
	public void ComposeTeaser_WithGraphemes()
	{
		var profile = new PlatformTextProfile(20, TextLengthMode.Graphemes, false, false, false);

		var result = CrossPostComposer.Compose(
			"\U0001F44D\U0001F44D\U0001F44D\U0001F44D\U0001F44D привет мир как дела",
			CrossPostFormat.Teaser,
			null,
			"\U0001F447",
			profile);

		result.Parts.ShouldBe(new[] { "\U0001F44D\U0001F44D\U0001F44D\U0001F44D\U0001F44D привет мир…\n\n\U0001F447" });
		result.Truncated.ShouldBeTrue();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, profile);
	}

	[Theory]
	[InlineData("Суперкалифраджилистикэкспиалидоций", "t.me/c/1", "Далее:", new[] { "Суперкалифраджилистикэ…\n\nДалее:\nt.me/c/1" }, true, false)]
	[InlineData("Да Суперкалифраджилистикэкспиалидоций", "t.me/c/1", "Далее:", new[] { "Да Суперкалифраджилист…\n\nДалее:\nt.me/c/1" }, true, false)]
	public void ComposeTeaser_LongWords(string text, string? linkText, string callToAction, string[] expectedParts, bool truncated, bool fellBack)
	{
		var result = CrossPostComposer.Compose(text, CrossPostFormat.Teaser, linkText, callToAction, P40);

		result.Parts.ShouldBe(expectedParts);
		result.Truncated.ShouldBe(truncated);
		result.FellBackToTeaser.ShouldBe(fellBack);
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void ComposeFull_EmptyText_ReturnsFooterOnly()
	{
		var result = CrossPostComposer.Compose("", CrossPostFormat.Full, "t.me/c", "Ещё:", P40);

		result.Parts.ShouldBe(new[] { "Ещё:\nt.me/c" });
		result.Truncated.ShouldBeFalse();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, P40);
	}

	[Fact]
	public void Compose_Guard_TruncatesFooterToMaxLength()
	{
		var longFooter = new string('x', 50);
		var profile = new PlatformTextProfile(40, TextLengthMode.Utf16, true, false, false);

		var result = CrossPostComposer.Compose("Короткий пост", CrossPostFormat.Teaser, null, longFooter, profile);

		result.Parts.Count.ShouldBe(1);
		result.Parts[0].Length.ShouldBe(40);
		result.Truncated.ShouldBeTrue();
		result.FellBackToTeaser.ShouldBeFalse();
		AssertPartsWithinLimit(result, profile);
	}

	private static void AssertPartsWithinLimit(CrossPostComposition composition, PlatformTextProfile profile)
	{
		foreach (var part in composition.Parts)
		{
			TextLength.Measure(part, profile.Mode).ShouldBeLessThanOrEqualTo(profile.MaxLength);
		}
	}
}
