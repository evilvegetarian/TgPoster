using Shouldly;
using TgPoster.Telegram.Models;

namespace Shared.Tests.Telegram;

public sealed class UsedTelegramChatsShould
{
	[Theory]
	[InlineData(1234567890L)]
	[InlineData(-1001234567890L)]
	[InlineData(-1234567890L)]
	public void MatchChatId_RegardlessOfPrefix(long storedId)
	{
		var sut = new UsedTelegramChats([storedId], []);

		var result = sut.Contains(1234567890, null);

		result.ShouldBeTrue();
	}

	[Fact]
	public void MatchRawIdAgainstPrefixedLookup()
	{
		var sut = new UsedTelegramChats([1234567890], []);

		var result = sut.Contains(-1001234567890, null);

		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData("somechannel")]
	[InlineData("@somechannel")]
	[InlineData("https://t.me/somechannel")]
	[InlineData("t.me/somechannel/123")]
	[InlineData("HTTPS://T.ME/SomeChannel?start=1")]
	[InlineData("  @SomeChannel  ")]
	public void MatchUsername_RegardlessOfFormat(string stored)
	{
		var sut = new UsedTelegramChats([], [stored]);

		var result = sut.Contains(42, "SomeChannel");

		result.ShouldBeTrue();
	}

	[Fact]
	public void NotMatchOtherChat()
	{
		var sut = new UsedTelegramChats([111], ["usedchannel"]);

		var result = sut.Contains(222, "otherchannel");

		result.ShouldBeFalse();
	}

	[Fact]
	public void NotMatchChatWithoutUsername_WhenOnlyUsernamesKnown()
	{
		var sut = new UsedTelegramChats([], ["usedchannel", "", "   "]);

		var result = sut.Contains(222, null);

		result.ShouldBeFalse();
	}
}
