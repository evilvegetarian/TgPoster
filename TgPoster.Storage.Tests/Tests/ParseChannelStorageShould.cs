using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ParseChannelStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ParseChannelStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task AddParseChannelParametersAsync_WithValidData_ShouldCreateChannelParsingParameters()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var channel = "TestChannel";
		var alwaysCheckNewPosts = true;
		var deleteText = false;
		var deleteMedia = true;
		var avoidWords = new[] { "spam", "ban", "fake" };
		var needVerifiedPosts = true;
		var dateFrom = DateTime.UtcNow.AddDays(-7);
		var dateTo = DateTime.UtcNow.AddDays(7);

		var result = await sut.AddParseChannelParametersAsync(
			channel,
			alwaysCheckNewPosts,
			schedule.Id,
			deleteText,
			deleteMedia,
			avoidWords,
			needVerifiedPosts,
			dateFrom,
			dateTo,
			true,
			CancellationToken.None);

		result.ShouldNotBe(Guid.Empty);

		var createdParameters = await context.ChannelParsingParameters
			.FirstOrDefaultAsync(x => x.Id == result);

		createdParameters.ShouldNotBeNull();
		createdParameters.Channel.ShouldBe(channel);
		createdParameters.CheckNewPosts.ShouldBe(alwaysCheckNewPosts);
		createdParameters.ScheduleId.ShouldBe(schedule.Id);
		createdParameters.DeleteText.ShouldBe(deleteText);
		createdParameters.DeleteMedia.ShouldBe(deleteMedia);
		createdParameters.AvoidWords.ShouldBe(avoidWords);
		createdParameters.NeedVerifiedPosts.ShouldBe(needVerifiedPosts);
		createdParameters.Status.ShouldBe(ParsingStatus.New);
	}

	[Fact]
	public async Task AddParseChannelParametersAsync_WithNullDates_ShouldCreateParametersWithNullDates()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var channel = "TestChannel";
		var avoidWords = new[] { "spam" };

		var result = await sut.AddParseChannelParametersAsync(
			channel,
			false,
			schedule.Id,
			false,
			false,
			avoidWords,
			false,
			null,
			null,
			true,
			CancellationToken.None);

		result.ShouldNotBe(Guid.Empty);

		var createdParameters = await context.ChannelParsingParameters
			.FirstOrDefaultAsync(x => x.Id == result);

		createdParameters.ShouldNotBeNull();
		createdParameters.DateFrom.ShouldBeNull();
		createdParameters.DateTo.ShouldBeNull();
	}

	[Fact]
	public async Task AddParseChannelParametersAsync_WithEmptyAvoidWords_ShouldCreateParameters()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var channel = "TestChannel";
		var avoidWords = Array.Empty<string>();

		var result = await sut.AddParseChannelParametersAsync(
			channel,
			false,
			schedule.Id,
			false,
			false,
			avoidWords,
			false,
			null,
			null,
			true,
			CancellationToken.None);

		result.ShouldNotBe(Guid.Empty);

		var createdParameters = await context.ChannelParsingParameters
			.FirstOrDefaultAsync(x => x.Id == result);

		createdParameters.ShouldNotBeNull();
		createdParameters.AvoidWords.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetTelegramTokenAsync_WithExistingSchedule_ShouldReturnToken()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetTelegramTokenAsync(schedule.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task GetTelegramTokenAsync_WithNonExistingSchedule_ShouldReturnNull()
	{
		var nonExistingScheduleId = Guid.NewGuid();

		var result = await sut.GetTelegramTokenAsync(nonExistingScheduleId, Guid.Empty, CancellationToken.None);

		result.ShouldBeNull();
	}
}