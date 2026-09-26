using System.Text;

namespace Shared.Social.Bluesky;

/// <summary>
///     Хелперы для построения richtext-фасеток Bluesky
/// </summary>
public static class BlueskyRichText
{
	/// <summary>
	///     Построить фасетки для всех вхождений linkText в текст
	/// </summary>
	/// <param name="text"></param>
	/// <param name="linkText"></param>
	/// <param name="uri"></param>
	/// <returns></returns>
	public static IReadOnlyList<BlueskyFacet> BuildLinkFacets(string text, string linkText, string uri)
	{
		if (string.IsNullOrEmpty(linkText))
		{
			return [];
		}

		var facets = new List<BlueskyFacet>();
		var startIndex = 0;

		while (true)
		{
			var index = text.IndexOf(linkText, startIndex, StringComparison.Ordinal);
			if (index < 0)
			{
				break;
			}

			var byteStart = Encoding.UTF8.GetByteCount(text.AsSpan(0, index));
			var byteEnd = byteStart + Encoding.UTF8.GetByteCount(linkText);

			facets.Add(new BlueskyFacet(byteStart, byteEnd, uri));
			startIndex = index + linkText.Length;
		}

		return facets;
	}
}
