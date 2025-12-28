using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class UpdateParseChannelStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly UpdateParseChannelStorage sut = new(fixture.GetDbContext());

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
		var parseChannel = new ChannelParsingSettingBuilder(_context).Create();
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
		var parseChannel = new ChannelParsingSettingBuilder(_context).Create();
		parseChannel.AvoidWords = avoids;
		_context.ChangeTracker.Clear();
		await sut.UpdateParseChannelAsync(parseChannel.ToCommand(), ct);
		var parseChannelUpdated =
			await _context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == parseChannel.Id, ct);
		parseChannelUpdated!.AvoidWords.ShouldBe(avoids);
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
			request.UseAiForPosts);
}