using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
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
		await sut.SaveClassifierSettingsAsync(Command() with { Model = "first/model" }, CancellationToken.None);
		await sut.SaveClassifierSettingsAsync(
			Command() with { Model = "second/model", Categories = ["Один", "Два"], ReclassifyAfterDays = 14 },
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
	public async Task GetClassifierSettingsAsync_ShouldReturnSelectedSession()
	{
		var session = new TelegramSessionBuilder(context).WithName("Классификатор").WithIsActive(false).Create();
		await sut.SaveClassifierSettingsAsync(
			Command() with { TelegramSessionId = session.Id }, CancellationToken.None);

		var result = await sut.GetClassifierSettingsAsync(CancellationToken.None);
		var savedSessionId = await sut.GetTelegramSessionIdAsync(CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramSession.ShouldNotBeNull();
		result.TelegramSession.Id.ShouldBe(session.Id);
		result.TelegramSession.Name.ShouldBe("Классификатор");
		result.TelegramSession.IsActive.ShouldBeFalse();
		savedSessionId.ShouldBe(session.Id);
	}

	[Fact]
	public async Task TelegramSessionBelongsToUserAsync_ShouldCheckOwner()
	{
		var session = new TelegramSessionBuilder(context).Create();

		var own = await sut.TelegramSessionBelongsToUserAsync(session.UserId, session.Id, CancellationToken.None);
		var foreign = await sut.TelegramSessionBelongsToUserAsync(Guid.NewGuid(), session.Id, CancellationToken.None);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
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
		null);
}
