namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

public interface IClassifyChannelStorage
{
	Task<List<ChannelForClassificationDto>> GetUnclassifiedChannelsAsync(int batchSize, CancellationToken ct);

	Task UpdateClassificationAsync(
		Guid id,
		string? category,
		string? subcategory,
		string[]? tags,
		string? language,
		double? confidence,
		CancellationToken ct);
}

public sealed class ChannelForClassificationDto
{
	public required Guid Id { get; init; }
	public required string? Title { get; init; }
	public required string? Description { get; init; }
}
