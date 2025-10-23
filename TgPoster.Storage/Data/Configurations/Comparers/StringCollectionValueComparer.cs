using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TgPoster.Storage.Data.Configurations.Comparers;

internal class StringCollectionValueComparer : ValueComparer<ICollection<string>>
{
	internal StringCollectionValueComparer()
		: base(
			(left, right) => (left == null && right == null)
			                 || (left != null && right != null && left.SequenceEqual(right)),
			list => list.Aggregate(0,
				(hash, item) => HashCode.Combine(hash, !string.IsNullOrWhiteSpace(item) ? item.GetHashCode() : 0)),
			list => list.ToList())
	{
	}
}