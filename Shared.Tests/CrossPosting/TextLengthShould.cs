using Shared.CrossPosting;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для класса TextLength
/// </summary>
public sealed class TextLengthShould
{
	[Theory]
	[InlineData("Привет", TextLengthMode.Utf16, 6)]
	[InlineData("Привет", TextLengthMode.Graphemes, 6)]
	[InlineData("Привет", TextLengthMode.ThreadsChars, 6)]
	[InlineData("\U0001F44D", TextLengthMode.Utf16, 2)]
	[InlineData("\U0001F44D", TextLengthMode.Graphemes, 1)]
	[InlineData("\U0001F44D", TextLengthMode.ThreadsChars, 4)]
	[InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467", TextLengthMode.Utf16, 8)]
	[InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467", TextLengthMode.Graphemes, 1)]
	[InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467", TextLengthMode.ThreadsChars, 18)]
	[InlineData("e\u0301", TextLengthMode.Utf16, 2)]
	[InlineData("e\u0301", TextLengthMode.Graphemes, 1)]
	[InlineData("e\u0301", TextLengthMode.ThreadsChars, 3)]
	[InlineData("\u2764", TextLengthMode.Utf16, 1)]
	[InlineData("\u2764", TextLengthMode.Graphemes, 1)]
	[InlineData("\u2764", TextLengthMode.ThreadsChars, 1)]
	[InlineData("\u2764\uFE0F", TextLengthMode.Utf16, 2)]
	[InlineData("\u2764\uFE0F", TextLengthMode.Graphemes, 1)]
	[InlineData("\u2764\uFE0F", TextLengthMode.ThreadsChars, 6)]
	[InlineData("", TextLengthMode.Utf16, 0)]
	[InlineData("", TextLengthMode.Graphemes, 0)]
	[InlineData("", TextLengthMode.ThreadsChars, 0)]
	public void MeasureLength(string text, TextLengthMode mode, int expected)
	{
		var result = TextLength.Measure(text, mode);

		result.ShouldBe(expected);
	}

	[Fact]
	public void SplitTextIntoSeparateElements()
	{
		var elements = TextLength.SplitTextElements("\U0001F44Da");

		elements.Count.ShouldBe(2);
		elements[0].ShouldBe("\U0001F44D");
		elements[1].ShouldBe("a");
	}

	[Fact]
	public void ReturnEmptyList_WhenTextIsEmpty()
	{
		var elements = TextLength.SplitTextElements("");

		elements.ShouldBeEmpty();
	}
}
