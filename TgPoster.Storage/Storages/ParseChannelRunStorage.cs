using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Storages;

internal sealed class ParseChannelRunStorage(PosterContext context, GuidFactory guidFactory) : IParseChannelRunStorage
{
	private const int MaxErrorLength = 2000;

	public async Task AddRunAsync(ParseChannelRunReport report, CancellationToken ct)
	{
		context.ChannelParsingRuns.Add(new ChannelParsingRun
		{
			Id = guidFactory.New(),
			ChannelParsingSettingId = report.ChannelParsingSettingId,
			StartedAt = report.StartedAt,
			FinishedAt = report.FinishedAt,
			Status = report.Status,
			Error = report.Error is { Length: > MaxErrorLength } error ? error[..MaxErrorLength] : report.Error,
			FetchedMessages = report.FetchedMessages,
			Posts = report.Posts,
			SkippedByAvoidWords = report.SkippedByAvoidWords,
			SkippedAsAds = report.SkippedAsAds,
			SkippedAsDuplicates = report.SkippedAsDuplicates,
			SkippedEmpty = report.SkippedEmpty,
			Published = report.Published,
			LastMessageId = report.LastMessageId
		});
		await context.SaveChangesAsync(ct);
	}
}
