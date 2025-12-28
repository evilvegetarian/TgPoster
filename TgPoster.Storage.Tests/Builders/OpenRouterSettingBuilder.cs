using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public class OpenRouterSettingBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly OpenRouterSetting setting = new()
	{
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		Id = Guid.NewGuid(),
		Model = faker.Lorem.Word(),
		TokenHash = faker.Random.Guid().ToString(),
		UserId = new UserBuilder(context).Create().Id
	};

	public OpenRouterSetting Build() => setting;

	public OpenRouterSetting Create()
	{
		context.OpenRouterSettings.Add(setting);
		context.SaveChanges();
		return setting;
	}

	public OpenRouterSettingBuilder WithScheduleId(Guid scheduleId)
	{
		setting.ScheduleId = scheduleId;
		return this;
	}

	public OpenRouterSettingBuilder WithSchedule(Schedule schedule)
	{
		setting.ScheduleId = schedule.Id;
		setting.Schedule = schedule;
		return this;
	}

	public OpenRouterSettingBuilder WithUser(User value)
	{
		setting.UserId = value.Id;
		setting.User = value;
		return this;
	}
}