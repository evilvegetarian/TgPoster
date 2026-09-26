using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ClassifierSettingsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ClassifierSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task SaveClassifierSettingsAsync_ShouldCreateThenUpdateSingleRecord()
	{
		var userId = new UserBuilder(context).Create().Id;

		await sut.SaveClassifierSettingsAsync(Command() with { Model = "first/model" }, userId, CancellationToken.None);
		await sut.SaveClassifierSettingsAsync(
			Command() with { Model = "second/model", Categories = ["Один", "Два"], ReclassifyAfterDays = 14 },
			userId,
			CancellationToken.None);

		var result = await sut.GetClassifierSettingsAsync(CancellationToken.None);

		result.ShouldNotBeNull();
		result.Model.ShouldBe("second/model");
		result.Categories.ShouldBe(["Один", "Два"]);
		result.ReclassifyAfterDays.ShouldBe(14);
		result.IsEnabled.ShouldBeFalse();
		result.BatchSize.ShouldBe(4);
		result.IntervalMinutes.ShouldBe(15);
		result.MessageSampleCount.ShouldBe(30);
		result.PhotoCount.ShouldBe(2);
		result.SystemPrompt.ShouldBe("Промпт {categories}");
		result.UpdatedAt.ShouldNotBeNull();
		(await context.ClassifierSettings.CountAsync(CancellationToken.None)).ShouldBe(1);
	}

	[Fact]
	public async Task SaveClassifierSettingsAsync_ShouldAssignPurposeToSelectedOwnSessionsOnly()
	{
		var userId = new UserBuilder(context).Create().Id;
		var selected = new TelegramSessionBuilder(context).WithUserId(userId)
			.WithPurposes(TelegramSessionPurpose.Discover).Create();
		var deselected = new TelegramSessionBuilder(context).WithUserId(userId)
			.WithPurposes(TelegramSessionPurpose.Classification, TelegramSessionPurpose.UpdateStats).Create();
		var foreign = new TelegramSessionBuilder(context)
			.WithPurposes(TelegramSessionPurpose.Classification).Create();

		await sut.SaveClassifierSettingsAsync(
			Command() with { TelegramSessionIds = [selected.Id] }, userId, CancellationToken.None);

		using var check = fixture.GetDbContext();
		var purposes = await check.TelegramSessions
			.Where(x => x.Id == selected.Id || x.Id == deselected.Id || x.Id == foreign.Id)
			.ToDictionaryAsync(x => x.Id, x => x.Purposes, CancellationToken.None);
		purposes[selected.Id].ShouldBe([TelegramSessionPurpose.Discover, TelegramSessionPurpose.Classification], true);
		purposes[deselected.Id].ShouldBe([TelegramSessionPurpose.UpdateStats]);
		purposes[foreign.Id].ShouldBe([TelegramSessionPurpose.Classification]);
	}

	[Fact]
	public async Task GetClassifierSessionsAsync_ShouldReturnOwnSessionsAndForeignAssignedOnes()
	{
		var userId = new UserBuilder(context).Create().Id;
		var ownSelected = new TelegramSessionBuilder(context).WithUserId(userId).WithName("Своя")
			.WithStatus(TelegramSessionStatus.Authorized)
			.WithPurposes(TelegramSessionPurpose.Classification).Create();
		var ownFree = new TelegramSessionBuilder(context).WithUserId(userId).WithIsActive(false).Create();
		var foreignAssigned = new TelegramSessionBuilder(context).WithName("Чужая")
			.WithPurposes(TelegramSessionPurpose.Classification).Create();
		var foreignFree = new TelegramSessionBuilder(context).Create();

		var result = await sut.GetClassifierSessionsAsync(userId, CancellationToken.None);

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

	private static UpdateClassifierSettingsCommand Command() => new(
		false,
		"some/model",
		4,
		15,
		30,
		2,
		null,
		["Технологии"],
		"Промпт {categories}",
		[]);
}
