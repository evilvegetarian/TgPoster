namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

/// <summary>
///     Ответ, содержащий пагинированную коллекцию.
/// </summary>
/// <typeparam name="T">Тип элементов в коллекции.</typeparam>
public sealed record PagedResponse<T>
{
	public PagedResponse(List<T> data, int totalCount, int currentPage, int pageSize)
	{
		Data = data;
		TotalCount = totalCount;
		PageSize = pageSize;
		CurrentPage = currentPage;
		TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
	}

	public int CurrentPage { get; }
	public int TotalPages { get; }
	public int PageSize { get; }
	public int TotalCount { get; }
	public bool HasPreviousPage => CurrentPage > 1;
	public bool HasNextPage => CurrentPage < TotalPages;
	public List<T> Data { get; }
}