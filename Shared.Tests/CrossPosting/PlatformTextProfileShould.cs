using Shared.CrossPosting;
using Shared.Enums;
using Shouldly;

namespace Shared.Tests.CrossPosting;

/// <summary>
///     Тесты для класса PlatformTextProfile
/// </summary>
public sealed class PlatformTextProfileShould
{
	[Fact]
	public void ReturnBlueskyProfile()
	{
		var profile = PlatformTextProfile.For(SocialPlatform.Bluesky);

		profile.MaxLength.ShouldBe(300);
		profile.Mode.ShouldBe(TextLengthMode.Graphemes);
		profile.SupportsChain.ShouldBeTrue();
		profile.ShortLinkText.ShouldBeTrue();
		profile.RequiresMedia.ShouldBeFalse();
	}

	[Fact]
	public void Throw_WhenPlatformIsUnknown()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => PlatformTextProfile.For((SocialPlatform)999));
	}
}
