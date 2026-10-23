using Shouldly;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Worker.Domain.Tests.ScheduleAlerts;

public sealed class ScheduleAlertServiceShould
{
	private static readonly ScheduleAlertTarget Target = new("encrypted-token", 555, "Новости", "Канал новостей");

	[Fact]
	public void BuildMessage_WithScheduleProblemAndDetails()
	{
		var result = ScheduleAlertService.BuildMessage(Target, ScheduleProblem.BotLostAccess, "403: Forbidden");

		var lines = result.Split('\n');
		lines.Length.ShouldBe(3);
		lines[0].ShouldBe("⚠️ Расписание «Новости» (Канал новостей)");
		lines[1].ShouldContain("Бот потерял доступ к каналу");
		lines[2].ShouldBe("403: Forbidden");
	}

	[Fact]
	public void BuildMessage_WithoutDetails_WhenDetailsEmpty()
	{
		var result = ScheduleAlertService.BuildMessage(Target, ScheduleProblem.PostFailed, " ");

		result.Split('\n').Length.ShouldBe(2);
	}

	[Fact]
	public void BuildMessage_WithTruncatedDetails_WhenDetailsTooLong()
	{
		var details = new string('а', 600);

		var result = ScheduleAlertService.BuildMessage(Target, ScheduleProblem.PostFailed, details);

		var detailsLine = result.Split('\n')[2];
		detailsLine.Length.ShouldBe(500);
		detailsLine.ShouldEndWith("…");
	}

	[Theory]
	[InlineData(ScheduleProblem.PostFailed)]
	[InlineData(ScheduleProblem.BotLostAccess)]
	[InlineData(ScheduleProblem.PostsInterrupted)]
	[InlineData(ScheduleProblem.QueueRunningLow)]
	[InlineData(ScheduleProblem.QueueEmpty)]
	[InlineData(ScheduleProblem.ParsingFailed)]
	public void BuildMessage_WithKnownDescription_ForEveryProblem(ScheduleProblem problem)
	{
		var result = ScheduleAlertService.BuildMessage(Target, problem, "details");

		result.ShouldNotContain("Неизвестная проблема");
	}

	[Fact]
	public void Truncate_KeepsSurrogatePairWhole()
	{
		var text = new string('а', 8) + "😀" + "хвост";

		var result = ScheduleAlertService.Truncate(text, 10);

		result.ShouldBe(new string('а', 8) + "…");
	}

	[Fact]
	public void Truncate_ReturnsSameText_WhenShortEnough()
	{
		var result = ScheduleAlertService.Truncate("короткий", 10);

		result.ShouldBe("короткий");
	}
}
