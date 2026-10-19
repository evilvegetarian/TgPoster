using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class RefreshDestinationInfoStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly RefreshDestinationInfoStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task AddAccountBanAsync_ShouldRecordBanOfSessionInChat()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();

		await sut.AddAccountBanAsync(session.Id, 1375846471, "недоступен", CancellationToken.None);

		var ban = await context.TelegramSessionChatBans
			.AsNoTracking()
			.SingleAsync(x => x.TelegramSessionId == session.Id);
		ban.ChatId.ShouldBe(1375846471);
		ban.Error.ShouldBe("недоступен");
	}

	[Fact]
	public async Task UpdateDestinationInfoAsync_WithoutStatus_ShouldKeepMembership()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithChatStatus(ChatStatus.Active)
			.CreateAsync();

		await sut.UpdateDestinationInfoAsync(
			destination.Id, "Канал", null, 10, ChatType.Channel, null, null, CancellationToken.None);

		var updated = await context.RepostDestinations.AsNoTracking().SingleAsync(x => x.Id == destination.Id);
		updated.ChatStatus.ShouldBe(ChatStatus.Active);
		updated.Title.ShouldBe("Канал");
	}

	[Fact]
	public async Task AddAccountBanAsync_WhenBanAlreadyRecorded_ShouldNotDuplicate()
	{
		var ban = await new TelegramSessionChatBanBuilder(context).CreateAsync();

		await sut.AddAccountBanAsync(ban.TelegramSessionId, ban.ChatId, "повторно", CancellationToken.None);

		var bans = await context.TelegramSessionChatBans
			.AsNoTracking()
			.Where(x => x.TelegramSessionId == ban.TelegramSessionId)
			.ToListAsync();
		bans.ShouldHaveSingleItem().Error.ShouldBe("повторно");
	}
}
