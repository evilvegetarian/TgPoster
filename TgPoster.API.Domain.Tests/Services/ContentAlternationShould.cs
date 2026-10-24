using Shouldly;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.Tests.Services;

public sealed class ContentAlternationShould
{
	private const int MaxInRow = 2;

	private static readonly Guid?[] Sources =
	[
		null,
		new Guid("00000000-0000-0000-0000-000000000001"),
		new Guid("00000000-0000-0000-0000-000000000002"),
		new Guid("00000000-0000-0000-0000-000000000003"),
		new Guid("00000000-0000-0000-0000-000000000004")
	];

	[Fact]
	public void ReturnPermutationOfItems_WhenItemsAreMixed()
	{
		var items = Generate(new Random(7), 40);

		var result = Arrange(items, new Random(7));

		result.OrderBy(x => x.Id).ShouldBe(items);
	}

	[Fact]
	public void NotPutMoreThanMaxInRow_WhenArrangementIsPossible()
	{
		for (var seed = 0; seed < 200; seed++)
		{
			var random = new Random(seed);
			List<Post> items;
			do
			{
				items = Generate(random, random.Next(1, 41));
			} while (!IsArrangeable(items));

			var result = Arrange(items, random);

			result.ShouldBe(items, true, $"seed {seed}");
			HasViolation(result).ShouldBeFalse($"seed {seed}: {Describe(result)}");
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void AvoidDeadEnd_WhenGreedyChoiceBlocksAllNextItems(int seed)
	{
		List<Post> items =
		[
			new(0, ContentKind.Video, Sources[1]),
			new(1, ContentKind.Video, Sources[1]),
			new(2, ContentKind.Video, null),
			new(3, ContentKind.Text, Sources[1])
		];

		var result = Arrange(items, new Random(seed));

		HasViolation(result).ShouldBeFalse(Describe(result));
	}

	[Fact]
	public void ReturnAllItems_WhenAllOfSameKindAndSource()
	{
		var items = Enumerable.Range(0, 50).Select(i => new Post(i, ContentKind.Video, Sources[1])).ToList();

		var result = Arrange(items, new Random(1));

		result.OrderBy(x => x.Id).ShouldBe(items);
	}

	[Fact]
	public void ReturnEmpty_WhenNoItems()
	{
		var result = Arrange([], new Random(1));

		result.ShouldBeEmpty();
	}

	[Fact]
	public void ReturnSingleItem_WhenOneItem()
	{
		var item = new Post(0, ContentKind.Photo, null);

		var result = Arrange([item], new Random(1));

		result.ShouldHaveSingleItem().ShouldBe(item);
	}

	private static List<Post> Arrange(IReadOnlyList<Post> items, Random random)
	{
		return ContentAlternation.Arrange(items, x => x.Kind, x => x.Source, MaxInRow, random);
	}

	private static List<Post> Generate(Random random, int count)
	{
		var sourceCount = random.Next(1, Sources.Length + 1);
		var dominantKind = (ContentKind)random.Next(3);
		var dominantSource = Sources[random.Next(sourceCount)];
		return Enumerable.Range(0, count)
			.Select(i => new Post(
				i,
				random.Next(3) == 0 ? dominantKind : (ContentKind)random.Next(3),
				random.Next(3) == 0 ? dominantSource : Sources[random.Next(sourceCount)]))
			.ToList();
	}

	private static bool IsArrangeable(List<Post> items)
	{
		var limit = items.Count / 2 + 1;
		return items.CountBy(x => x.Kind).All(x => x.Value <= limit)
		       && items.CountBy(x => x.Source ?? Guid.Empty).All(x => x.Value <= limit);
	}

	private static bool HasViolation(List<Post> posts)
	{
		for (var i = MaxInRow; i < posts.Count; i++)
		{
			var window = posts.Skip(i - MaxInRow).Take(MaxInRow + 1).ToList();
			if (window.DistinctBy(x => x.Kind).Count() == 1 || window.DistinctBy(x => x.Source).Count() == 1)
			{
				return true;
			}
		}

		return false;
	}

	private static string Describe(List<Post> posts)
	{
		return string.Join(" ", posts.Select(x => $"{x.Kind}/{Array.IndexOf(Sources, x.Source)}"));
	}

	private sealed record Post(int Id, ContentKind Kind, Guid? Source);
}
