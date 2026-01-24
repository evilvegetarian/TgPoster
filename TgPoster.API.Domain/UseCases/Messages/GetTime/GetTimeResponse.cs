namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

public sealed record GetTimeResponse
{
	public List<DateTimeOffset> PostingTimes { get; init; } = [];
}