namespace TgPoster.API.Domain.Models;

/// <summary>
///     Внутренний класс для передачи пагинированных данных из хранилища.
/// </summary>
public class PagedList<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }

    public PagedList(List<T> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}