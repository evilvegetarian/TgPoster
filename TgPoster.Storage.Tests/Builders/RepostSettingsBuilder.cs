using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public sealed class RepostSettingsBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly RepostSettings settings = new()
	{
		Id = Guid.NewGuid(),
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		TelegramSessionId = new TelegramSessionBuilder(context).Create().Id,
		IsActive = true
	};

	public RepostSettings Build() => settings;

	public RepostSettings Create()
	{
		context.Add(settings);
		context.SaveChanges();
		return settings;
	}

	public async Task<RepostSettings> CreateAsync(CancellationToken ct = default)
	{
		await context.AddAsync(settings, ct);
		await context.SaveChangesAsync(ct);
		return settings;
	}

	public RepostSettingsBuilder WithScheduleId(Guid scheduleId)
	{
		settings.ScheduleId = scheduleId;
		return this;
	}

	public RepostSettingsBuilder WithSchedule(Schedule schedule)
	{
		settings.ScheduleId = schedule.Id;
		settings.Schedule = schedule;
		return this;
	}

	public RepostSettingsBuilder WithTelegramSessionId(Guid telegramSessionId)
	{
		settings.TelegramSessionId = telegramSessionId;
		return this;
	}

	public RepostSettingsBuilder WithTelegramSession(TelegramSession session)
	{
		settings.TelegramSessionId = session.Id;
		settings.TelegramSession = session;
		return this;
	}

	public RepostSettingsBuilder WithIsActive(bool isActive)
	{
		settings.IsActive = isActive;
		return this;
	}
}
