namespace TgPoster.API.Domain.Services;

/// <summary>
/// Тип содержимого поста при чередовании
/// </summary>
public enum ContentKind
{
	Text,
	Photo,
	Video
}

/// <summary>
/// Случайная раскладка постов, в которой подряд идёт ограниченное число постов одного типа и одного источника
/// </summary>
internal static class ContentAlternation
{
	// Сколько состояний перебора можно пройти, прежде чем считать раскладку без нарушний ненайденной
	private const int SearchBudget = 100_000;

	/// <summary>
	/// Расставляет элементы в случайном порядке так, чтобы подряд шло не больше <paramref name="maxInRow"/>
	/// элементов одного типа и не больше <paramref name="maxInRow"/> элементов одного источника, если такая
	/// раскладка возможна. Элементы без источника считаются одной группой. На каждом шаге берётся элемент,
	/// не удлиняющий серию сверх предела, у которого больше всего оставшихся элементов того же типа и того же
	/// источника, при равенстве — первый в перемешанном порядке. Если выбор заводит в тупик, перебор
	/// возвращается назад; если раскладки без нарушений нет, правило нарушается там, где допустимых элементов
	/// не осталось
	/// </summary>
	/// <param name="items"></param>
	/// <param name="kind"></param>
	/// <param name="source"></param>
	/// <param name="maxInRow"></param>
	/// <param name="random"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static List<T> Arrange<T>(
		IReadOnlyList<T> items,
		Func<T, ContentKind> kind,
		Func<T, Guid?> source,
		int maxInRow,
		Random random
	)
	{
		var shuffled = items.ToArray();
		random.Shuffle(shuffled);

		var groups = shuffled
			.Select((item, position) => (Kind: kind(item), Source: source(item), Position: position))
			.GroupBy(x => (x.Kind, x.Source), x => x.Position)
			.Select(g => new Group(g.Key.Kind, g.Key.Source, [..g]))
			.ToArray();

		var order = new Layout(groups, maxInRow).Search() ?? new Layout(groups, maxInRow).Greedy();

		var taken = new int[groups.Length];
		return order.Select(g => shuffled[groups[g].Positions[taken[g]++]]).ToList();
	}

	// Ручные посты без источника считаются одним источником
	private static Guid SourceKey(Guid? source) => source ?? Guid.Empty;

	/// <summary>
	/// Элементы одного типа и одного источника: для правила они взаимозаменяемы, поэтому берутся
	/// в перемешанном порядке
	/// </summary>
	/// <param name="Kind"></param>
	/// <param name="Source"></param>
	/// <param name="Positions"></param>
	private sealed record Group(ContentKind Kind, Guid? Source, int[] Positions);

	private sealed class Frame(int[] options)
	{
		public int[] Options { get; } = options;

		public int Next { get; set; }
	}

	/// <summary>
	/// Раскладка, которая строится по одному элементу с возможностью отменить последний шаг
	/// </summary>
	private sealed class Layout(Group[] groups, int maxInRow)
	{
		private readonly int[] left = groups.Select(x => x.Positions.Length).ToArray();

		private readonly Dictionary<ContentKind, int> kindLeft = groups
			.GroupBy(x => x.Kind)
			.ToDictionary(x => x.Key, x => x.Sum(g => g.Positions.Length));

		private readonly Dictionary<Guid, int> sourceLeft = groups
			.GroupBy(x => SourceKey(x.Source))
			.ToDictionary(x => x.Key, x => x.Sum(g => g.Positions.Length));

		private readonly List<int> path = [];
		private readonly Stack<(int KindRun, int SourceRun)> runs = new();
		private int kindRun;
		private int sourceRun;
		private int remaining = groups.Sum(x => x.Positions.Length);

		private Group? Last => path.Count > 0 ? groups[path[^1]] : null;

		/// <summary>
		/// Перебор с возвратом, где варианты на каждом шаге идут в порядке жадного выбора
		/// </summary>
		/// <returns></returns>
		public List<int>? Search()
		{
			var failed = new HashSet<string>();
			var frames = new Stack<Frame>();
			frames.Push(new Frame(Options()));
			var budget = SearchBudget;
			while (remaining > 0)
			{
				if (budget-- == 0)
				{
					return null;
				}

				var frame = frames.Peek();
				if (frame.Next == frame.Options.Length)
				{
					frames.Pop();
					if (frames.Count == 0)
					{
						return null;
					}

					failed.Add(StateKey());
					Undo();
					continue;
				}

				Take(frame.Options[frame.Next++]);
				frames.Push(new Frame(failed.Contains(StateKey()) ? [] : Options()));
			}

			return path;
		}

		/// <summary>
		/// Жадная раскладка без возврата: когда допустимых групп нет, берётся лучшая из всех
		/// </summary>
		/// <returns></returns>
		public List<int> Greedy()
		{
			while (remaining > 0)
			{
				var candidates = Enumerable.Range(0, groups.Length).Where(g => left[g] > 0).ToList();
				var fitting = candidates.Where(Fits).ToList();
				Take(Ordered(fitting.Count > 0 ? fitting : candidates).First());
			}

			return path;
		}

		private int[] Options()
		{
			if (!WithinBounds())
			{
				return [];
			}

			return Ordered(Enumerable.Range(0, groups.Length).Where(g => left[g] > 0 && Fits(g))).ToArray();
		}

		private IEnumerable<int> Ordered(IEnumerable<int> candidates)
		{
			return candidates
				.OrderByDescending(g => kindLeft[groups[g].Kind] + sourceLeft[SourceKey(groups[g].Source)])
				.ThenBy(g => groups[g].Positions[^left[g]]);
		}

		private bool Fits(int g)
		{
			var last = Last;
			if (last is null)
			{
				return true;
			}

			var kindFits = groups[g].Kind != last.Kind || kindRun < maxInRow;
			var sourceFits = groups[g].Source != last.Source || sourceRun < maxInRow;
			return kindFits && sourceFits;
		}

		// Необходимое условие: оставшиеся элементы одного типа или источника помещаются сериями по maxInRow
		// между остальными, с учётом уже начатой серии
		private bool WithinBounds()
		{
			var last = Last;
			foreach (var (kind, count) in kindLeft)
			{
				var firstRun = last is not null && last.Kind == kind ? maxInRow - kindRun : maxInRow;
				if (count > firstRun + maxInRow * (remaining - count))
				{
					return false;
				}
			}

			foreach (var (source, count) in sourceLeft)
			{
				var firstRun = last is not null && SourceKey(last.Source) == source ? maxInRow - sourceRun : maxInRow;
				if (count > firstRun + maxInRow * (remaining - count))
				{
					return false;
				}
			}

			return true;
		}

		private void Take(int g)
		{
			var last = Last;
			runs.Push((kindRun, sourceRun));
			kindRun = last is not null && last.Kind == groups[g].Kind ? kindRun + 1 : 1;
			sourceRun = last is not null && last.Source == groups[g].Source ? sourceRun + 1 : 1;
			path.Add(g);
			Move(g, -1);
		}

		private void Undo()
		{
			var g = path[^1];
			path.RemoveAt(path.Count - 1);
			(kindRun, sourceRun) = runs.Pop();
			Move(g, 1);
		}

		private void Move(int g, int delta)
		{
			left[g] += delta;
			kindLeft[groups[g].Kind] += delta;
			sourceLeft[SourceKey(groups[g].Source)] += delta;
			remaining += delta;
		}

		private string StateKey()
		{
			return $"{string.Join(',', left)}|{(path.Count > 0 ? path[^1] : -1)}|{kindRun}|{sourceRun}";
		}
	}
}
