namespace TgPoster.API.Models;

public class PaginationRequest
{
    private const int MaxPageSize = 50;
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    ///     Номер страницы.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = (value > 0) ? value : 1;
    }

    /// <summary>
    ///     Размер страницы.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}