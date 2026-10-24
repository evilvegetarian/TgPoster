using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ParseChannelRunStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ParseChannelRunStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task AddRunAsync_ShouldSaveReport()
	{
		var source = await new ChannelParsingSettingBuilder(context).CreateAsync();
		var startedAt = DateTimeOffset.UtcNow.AddMinutes(-2);

		await sut.AddRunAsync(new ParseChannelRunReport
		{
			ChannelParsingSettingId = source.Id,
			StartedAt = startedAt,
			FinishedAt = startedAt.AddMinutes(1),
			Status = ParseChannelRunStatus.Success,
			FetchedMessages = 40,
			Posts = 30,
			SkippedByAvoidWords = 1,
			SkippedAsAds = 3,
			SkippedAsDuplicates = 20,
			SkippedEmpty = 2,
			Published = 4,
			LastMessageId = 900
		}, CancellationToken.None);

		var run = await context.ChannelParsingRuns.AsNoTracking().SingleAsync(x => x.ChannelParsingSettingId == source.Id);
		run.Status.ShouldBe(ParseChannelRunStatus.Success);
		run.StartedAt.ShouldBe(startedAt, TimeSpan.FromMilliseconds(1));
		run.FetchedMessages.ShouldBe(40);
		run.SkippedAsDuplicates.ShouldBe(20);
		run.Published.ShouldBe(4);
		run.LastMessageId.ShouldBe(900);
	}

	[Fact]
	public async Task AddRunAsync_ShouldCutLongError()
	{
		var source = await new ChannelParsingSettingBuilder(context).CreateAsync();
		var now = DateTimeOffset.UtcNow;

		await sut.AddRunAsync(ParseChannelRunReport.Failed(source.Id, now, now, new string('x', 5000)),
			CancellationToken.None);

		var run = await context.ChannelParsingRuns.AsNoTracking().SingleAsync(x => x.ChannelParsingSettingId == source.Id);
		run.Status.ShouldBe(ParseChannelRunStatus.Failed);
		run.Error!.Length.ShouldBe(2000);
	}
}
