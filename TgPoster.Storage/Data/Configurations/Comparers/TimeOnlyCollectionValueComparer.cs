using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TgPoster.Storage.Data.Configurations.Comparers;

internal class TimeOnlyCollectionValueComparer : ValueComparer<ICollection<TimeOnly>>
{
    internal TimeOnlyCollectionValueComparer()
        : base( 
            (left, right) => (left == null && right == null) || (left != null && right != null && left.SequenceEqual(right)),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, (item == default) ? item.GetHashCode() : 0)),
            list => list.ToList())
    {
    }
}