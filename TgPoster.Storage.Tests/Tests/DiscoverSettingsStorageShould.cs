using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DiscoverSettingsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DiscoverSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task SaveDiscoverSettingsAsync_ShouldCreateThenUpdateSingleRecord()
	{
		var userId = new UserBuilder(context).Create().Id;

		await sut.SaveDiscoverSettingsAsync(new UpdateDiscoverSettingsCommand(true, 60, 1, []), userId,
			CancellationToken.None);
		await sut.SaveDiscoverSettingsAsync(new UpdateDiscoverSettingsCommand(false, 15, 3, []), userId,
			CancellationToken.None);

		var result = await sut.GetDiscoverSettingsAsync(CancellationToken.None);

		result.ShouldNotBeNull();
		result.IsEnabled.ShouldBeFalse();
		result.IntervalMinutes.ShouldBe(15);
		result.ChannelsPerSession.ShouldBe(3);
		result.UpdatedAt.ShouldNotBeNull();
		(await context.DiscoverSettings.CountAsync(CancellationToken.None)).ShouldBe(1);
	}

	[Fact]
	public async Task SaveDiscoverSettingsAsync_ShouldAssignPurposeToSelectedOwnSessionsOnly()
	{
		var userId = new UserBuilder(context).Create().Id;
		var selected = new TelegramSessionBuilder(context).WithUserId(userId)
			.WithPurposes(TelegramSessionPurpose.Classification).Create();
		var deselected = new TelegramSessionBuilder(context).WithUserId(userId)
			.WithPurposes(TelegramSessionPurpose.Discover, TelegramSessionPurpose.UpdateStats).Create();
		var foreign = new TelegramSessionBuilder(context)
			.WithPurposes(TelegramSessionPurpose.Discover).Create();

		await sut.SaveDiscoverSettingsAsync(
			new UpdateDiscoverSettingsCommand(true, 60, 1, [selected.Id]), userId, CancellationToken.None);

		using var check = fixture.GetDbContext();
		var purposes = await check.TelegramSessions
			.Where(x => x.Id == selected.Id || x.Id == deselected.Id || x.Id == foreign.Id)
			.ToDictionaryAsync(x => x.Id, x => x.Purposes, CancellationToken.None);
		purposes[selected.Id].ShouldBe([TelegramSessionPurpose.Classification, TelegramSessionPurpose.Discover], true);
		purposes[deselected.Id].ShouldBe([TelegramSessionPurpose.UpdateStats]);
		purposes[foreign.Id].ShouldBe([TelegramSessionPurpose.Discover]);
	}

	[Fact]
	public async Task GetDiscoverSessionsAsync_ShouldReturnOwnSessionsAndForeignAssignedOnes()
	{
		var userId = new UserBuilder(context).Create().Id;
		var ownSelected = new TelegramSessionBuilder(context).WithUserId(userId).WithName("Своя")
			.WithStatus(TelegramSessionStatus.Authorized)
			.WithPurposes(TelegramSessionPurpose.Discover).Create();
		var ownFree = new TelegramSessionBuilder(context).WithUserId(userId).WithIsActive(false)
			.WithPurposes(TelegramSessionPurpose.Classification).Create();
		var foreignAssigned = new TelegramSessionBuilder(context).WithName("Чужая")
			.WithPurposes(TelegramSessionPurpose.Discover).Create();
		var foreignFree = new TelegramSessionBuilder(context)
			.WithPurposes(TelegramSessionPurpose.Classification).Create();

		var result = await sut.GetDiscoverSessionsAsync(userId, CancellationToken.None);

		result.Select(x => x.Id).ShouldContain(ownSelected.Id);
		result.Select(x => x.Id).ShouldContain(ownFree.Id);
		result.Select(x => x.Id).ShouldContain(foreignAssigned.Id);
		result.Select(x => x.Id).ShouldNotContain(foreignFree.Id);
		var own = result.Single(x => x.Id == ownSelected.Id);
		own.IsOwn.ShouldBeTrue();
		own.IsSelected.ShouldBeTrue();
		own.IsAuthorized.ShouldBeTrue();
		own.PhoneNumber.ShouldBe(ownSelected.PhoneNumber);
		var free = result.Single(x => x.Id == ownFree.Id);
		free.IsSelected.ShouldBeFalse();
		free.IsActive.ShouldBeFalse();
		var foreign = result.Single(x => x.Id == foreignAssigned.Id);
		foreign.IsOwn.ShouldBeFalse();
		foreign.PhoneNumber.ShouldBeNull();
		foreign.Name.ShouldBe("Чужая");
		result.FindIndex(x => x.Id == foreignAssigned.Id)
			.ShouldBeGreaterThan(result.FindIndex(x => x.Id == ownFree.Id));
	}

	[Fact]
	public async Task GetUserSessionIdsAsync_ShouldReturnOnlyUserSessions()
	{
		var userId = new UserBuilder(context).Create().Id;
		var own = new TelegramSessionBuilder(context).WithUserId(userId).Create();
		var foreign = new TelegramSessionBuilder(context).Create();

		var result = await sut.GetUserSessionIdsAsync(userId, CancellationToken.None);

		result.ShouldBe([own.Id]);
		result.ShouldNotContain(foreign.Id);
	}
}
