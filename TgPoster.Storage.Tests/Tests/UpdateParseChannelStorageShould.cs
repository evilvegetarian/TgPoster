using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class UpdateParseChannelStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context;
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly UpdateParseChannelStorage sut;

	public UpdateParseChannelStorageShould(StorageTestFixture fixture)
	{
		_context = fixture.GetDbContext();
		sut = new UpdateParseChannelStorage(_context);
	}

	[Fact]
	public async Task ExistParseChannelAsync_ExistingParseChannel_ShouldReturnTrue()
	{
		var parseChannel = new ChannelParsingSettingBuilder(_context).Create();
		var exist = await sut.ExistParseChannelAsync(parseChannel.Id, parseChannel.Schedule.UserId, ct);
		exist.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistParseChannelAsync_NonExistingParseChannel_ShouldReturnFalse()
	{
		var parseChannel = await new ChannelParsingSettingBuilder(_context).CreateAsync(ct);
		var exist = await sut.ExistParseChannelAsync(Guid.NewGuid(), parseChannel.Schedule.UserId, ct);
		exist.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistParseChannelAsync_NonExistingUserId_ShouldReturnFalse()
	{
		var parseChannel = new ChannelParsingSettingBuilder(_context).Build();
		var exist = await sut.ExistParseChannelAsync(parseChannel.Id, Guid.NewGuid(), ct);
		exist.ShouldBeFalse();
	}


	[Fact]
	public async Task UpdateParseChannelAsync_NonExistingUserId_ShouldReturnFalse()
	{
		string[] avoids = ["New Word", "Perfectly"];
		var parseChannel = await new ChannelParsingSettingBuilder(_context).CreateAsync(ct);
		parseChannel.AvoidWords = avoids;
		_context.Entry(parseChannel).State = EntityState.Detached;
		await sut.UpdateParseChannelAsync(parseChannel.ToCommand(), ct);

		var parseChannelUpdated = await _context.ChannelParsingParameters
			.FirstOrDefaultAsync(x => x.Id == parseChannel.Id, ct);
		parseChannelUpdated!.AvoidWords.ShouldBe(avoids);
	}

	[Fact]
	public async Task UpdateParseChannelAsync_WithSkipAdsDisabled_ShouldSaveFalse()
	{
		var parseChannel = await new ChannelParsingSettingBuilder(_context).CreateAsync(ct);
		parseChannel.SkipAds = false;
		_context.Entry(parseChannel).State = EntityState.Detached;

		await sut.UpdateParseChannelAsync(parseChannel.ToCommand(), ct);

		var parseChannelUpdated = await _context.ChannelParsingParameters.AsNoTracking()
			.FirstAsync(x => x.Id == parseChannel.Id, ct);
		parseChannelUpdated.SkipAds.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateParseChannelAsync_WithSameChannel_ShouldKeepParsingPosition()
	{
		var source = await CreateParsedSourceAsync();
		var command = source.ToCommand() with { Channel = "@" + source.Channel, AvoidWords = ["новое"] };

		await sut.UpdateParseChannelAsync(command, ct);

		var updated = await _context.ChannelParsingParameters.AsNoTracking().FirstAsync(x => x.Id == source.Id, ct);
		updated.LastParseId.ShouldBe(500);
		updated.TotalMessagesCount.ShouldBe(985);
		updated.Status.ShouldBe(ParsingStatus.Waiting);
		updated.AvoidWords.ShouldBe(["новое"]);
	}

	[Fact]
	public async Task UpdateParseChannelAsync_WithAnotherChannel_ShouldResetParsingPosition()
	{
		var source = await CreateParsedSourceAsync();
		var command = source.ToCommand() with { Channel = "another_channel" };

		await sut.UpdateParseChannelAsync(command, ct);

		var updated = await _context.ChannelParsingParameters.AsNoTracking().FirstAsync(x => x.Id == source.Id, ct);
		updated.Channel.ShouldBe("another_channel");
		updated.LastParseId.ShouldBeNull();
		updated.TotalMessagesCount.ShouldBeNull();
		updated.Status.ShouldBe(ParsingStatus.New);
	}

	private async Task<ChannelParsingSetting> CreateParsedSourceAsync()
	{
		var source = new ChannelParsingSettingBuilder(_context).WithStatus(ParsingStatus.Waiting).Build();
		source.LastParseId = 500;
		source.TotalMessagesCount = 985;
		_context.Add(source);
		await _context.SaveChangesAsync(ct);
		_context.Entry(source).State = EntityState.Detached;
		return source;
	}
}

public static class UpdateParseChannelStorageShouldExtensions
{
	public static UpdateParseChannelCommand ToCommand(this ChannelParsingSetting request) =>
		new(
			request.Id,
			request.Channel,
			request.CheckNewPosts,
			request.ScheduleId,
			request.DeleteText,
			request.DeleteMedia,
			request.AvoidWords,
			request.NeedVerifiedPosts,
			request.DateFrom,
			request.DateTo,
			request.UseAiForPosts,
			request.TelegramSessionId,
			request.SkipAds);
}