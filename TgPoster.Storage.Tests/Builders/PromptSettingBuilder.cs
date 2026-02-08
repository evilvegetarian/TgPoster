using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class PromptSettingBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly PromptSetting setting = new()
	{
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		Id = Guid.NewGuid()
	};

	public PromptSetting Build() => setting;

	public PromptSetting Create()
	{
		context.PromptSettings.Add(setting);
		context.SaveChanges();
		return setting;
	}

	public PromptSettingBuilder WithScheduleId(Guid scheduleId)
	{
		setting.ScheduleId = scheduleId;
		return this;
	}

	public PromptSettingBuilder WithSchedule(Schedule schedule)
	{
		setting.ScheduleId = schedule.Id;
		setting.Schedule = schedule;
		return this;
	}

	public PromptSettingBuilder WithVideoPrompt(string value)
	{
		setting.VideoPrompt = value;
		return this;
	}

	public PromptSettingBuilder WithTextPrompt(string value)
	{
		setting.TextPrompt = value;
		return this;
	}

	public PromptSettingBuilder WithPicturePrompt(string value)
	{
		setting.PicturePrompt = value;
		return this;
	}
}