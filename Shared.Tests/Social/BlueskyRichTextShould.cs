using Shared.Social.Bluesky;
using Shouldly;

namespace Shared.Tests.Social;

/// <summary>
///     Тесты для BlueskyRichText.BuildLinkFacets
/// </summary>
public sealed class BlueskyRichTextShould
{
	[Fact]
	public void BuildFacetForCyrillicPrefix()
	{
		var facets = BlueskyRichText.BuildLinkFacets("Далее:\nt.me/c/1", "t.me/c/1", "https://t.me/c/1");

		facets.Count.ShouldBe(1);
		facets[0].ByteStart.ShouldBe(12);
		facets[0].ByteEnd.ShouldBe(20);
		facets[0].Uri.ShouldBe("https://t.me/c/1");
	}

	[Fact]
	public void BuildFacetsForMultipleOccurrences()
	{
		var facets = BlueskyRichText.BuildLinkFacets("go t.me/c and t.me/c", "t.me/c", "https://t.me/c");

		facets.Count.ShouldBe(2);
		facets[0].ShouldBe(new BlueskyFacet(3, 9, "https://t.me/c"));
		facets[1].ShouldBe(new BlueskyFacet(14, 20, "https://t.me/c"));
	}

	[Fact]
	public void BuildFacetWithEmojiBeforeLink()
	{
		var text = "\ud83d\udc4d t.me/c";

		var facets = BlueskyRichText.BuildLinkFacets(text, "t.me/c", "https://t.me/c");

		facets.Count.ShouldBe(1);
		facets[0].ByteStart.ShouldBe(5);
		facets[0].ByteEnd.ShouldBe(11);
	}

	[Fact]
	public void ReturnEmptyListWhenLinkTextNotFound()
	{
		var facets = BlueskyRichText.BuildLinkFacets("нет ссылки", "t.me/c", "https://t.me/c");

		facets.ShouldBeEmpty();
	}

	[Fact]
	public void ReturnEmptyListWhenLinkTextEmpty()
	{
		var facets = BlueskyRichText.BuildLinkFacets("text", "", "https://t.me/c");

		facets.ShouldBeEmpty();
	}
}
