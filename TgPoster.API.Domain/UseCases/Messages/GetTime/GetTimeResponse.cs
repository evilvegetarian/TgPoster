namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

public class GetTimeResponse
{
	public List<DateTimeOffset> PostingTimes { get; set; } = [];
}