using Shared.CrossPosting;
using Shared.Enums;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для CallToActionPicker
/// </summary>
public sealed class CallToActionPickerShould
{
	[Theory]
	[InlineData(null, CrossPostFormat.Teaser, "Читать полностью в Telegram 👇")]
	[InlineData("", CrossPostFormat.Full, "Больше интересного — в нашем Telegram 👇")]
	[InlineData("   \n  ", CrossPostFormat.Announcement, "Новый пост в нашем Telegram-канале 👇")]
	[InlineData("Один", CrossPostFormat.Teaser, "Один")]
	public void PickCallToAction(string? variants, CrossPostFormat format, string expected)
	{
		var result = CallToActionPicker.Pick(variants, format, new Random(1));

		result.ShouldBe(expected);
	}

	[Fact]
	public void PickRandomVariant()
	{
		var random = new FixedRandom(1);

		var result = CallToActionPicker.Pick("А\n\nБ\r\nВ", CrossPostFormat.Teaser, random);

		result.ShouldBe("Б");
	}

	[Theory]
	[InlineData("А\n\nБ", CrossPostFormat.Teaser, "А")]
	[InlineData(null, CrossPostFormat.Full, "Больше интересного — в нашем Telegram 👇")]
	public void ReturnFirstVariant(string? variants, CrossPostFormat format, string expected)
	{
		var result = CallToActionPicker.First(variants, format);

		result.ShouldBe(expected);
	}

	private sealed class FixedRandom(int value) : Random
	{
		public override int Next(int maxValue) => value;
	}
}
