using Shared.CrossPosting;
using Shared.Enums;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для CrossPostLinkBuilder
/// </summary>
public sealed class CrossPostLinkBuilderShould
{
	[Fact]
	public void BuildPostLink()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Post, "channel", 123, null);

		result.ShouldBe("https://t.me/channel/123");
	}

	[Fact]
	public void BuildChannelLink()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Channel, "channel", 123, null);

		result.ShouldBe("https://t.me/channel");
	}

	[Fact]
	public void BuildCustomLink()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Custom, "channel", null, "https://example.com");

		result.ShouldBe("https://example.com");
	}

	[Fact]
	public void BuildCustomLink_ReturnsNull_WhenEmpty()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Custom, "channel", null, "   ");

		result.ShouldBeNull();
	}

	[Fact]
	public void BuildNone_ReturnsNull()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.None, "channel", 123, "https://example.com");

		result.ShouldBeNull();
	}

	[Fact]
	public void TrimAtSign_FromChannelName()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Post, "@channel", 123, null);

		result.ShouldBe("https://t.me/channel/123");
	}

	[Fact]
	public void BuildPostLink_FallsBackToChannel_WhenMessageIdMissing()
	{
		var result = CrossPostLinkBuilder.Build(CrossPostLinkTarget.Post, "channel", null, null);

		result.ShouldBe("https://t.me/channel");
	}

	[Fact]
	public void ShortenLink_ForBluesky()
	{
		var result = CrossPostLinkBuilder.ToLinkText("https://t.me/c/1", PlatformTextProfile.For(SocialPlatform.Bluesky));

		result.ShouldBe("t.me/c/1");
	}

	[Fact]
	public void KeepFullLink_WhenShortLinkTextDisabled()
	{
		var profile = new PlatformTextProfile(500, TextLengthMode.Utf16, true, false, false);

		var result = CrossPostLinkBuilder.ToLinkText("https://t.me/c/1", profile);

		result.ShouldBe("https://t.me/c/1");
	}
}
