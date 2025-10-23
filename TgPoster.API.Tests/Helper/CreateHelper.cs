using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Accounts.SignIn;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Models;

namespace TgPoster.Endpoint.Tests.Helper;

public class CreateHelper(HttpClient client)
{
	public async Task<Guid> CreateSchedule()
	{
		var request = new CreateScheduleRequest
		{
			Name = "Test Schedule",
			TelegramBotId = GlobalConst.Worked.TelegramBotId,
			Channel = GlobalConst.Worked.Channel
		};
		var response = await client.PostAsync<CreateScheduleResponse>(Routes.Schedule.Create, request);
		return response.Id;
	}

	public async Task CreateDay(Guid scheduleId, DayOfWeek? dayOfWeek)
	{
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = dayOfWeek ?? DayOfWeek.Monday,
					StartPosting = new TimeOnly(10, 15),
					EndPosting = new TimeOnly(20, 30),
					Interval = 45
				}
			]
		};
		var response = await client.PostAsync(Routes.Day.Root + "?scheduleId=" + scheduleId, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);
	}

	public async Task CreateDay()
	{
		var scheduleId = await CreateSchedule();
		await CreateDay(scheduleId, null);
	}

	public async Task<Guid> SignOn(string username, string password)
	{
		var signOnRequest = new SignOnRequest
		{
			Login = username,
			Password = password
		};
		var response = await client.PostAsync<SignOnResponse>(Routes.Account.SignOn, signOnRequest);
		return response.UserId;
	}

	public async Task<SignInResponse> SignIn(string username, string password)
	{
		var signOnRequest = new SignOnRequest
		{
			Login = username,
			Password = password
		};
		var response = await client.PostAsync<SignInResponse>(Routes.Account.SignIn, signOnRequest);
		return response;
	}

	public async Task CreateMessages(Guid scheduleId)
	{
		await CreateDay(scheduleId, null);
		var files = FileHelper.GetTestIFormFiles();
		var request = new CreateMessagesFromFilesRequest
		{
			ScheduleId = scheduleId,
			Files = files
		};

		var createResponse = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
	}

	public async Task<Guid> CreateParseChannel()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			AlwaysCheckNewPosts = true,
			DeleteText = false,
			AvoidWords = ["perfect"],
			DeleteMedia = true,
			ScheduleId = GlobalConst.Worked.ScheduleId
		};

		var createResponse = await client.PostAsync<CreateParseChannelResponse>(Routes.ParseChannel.Create, request);
		return createResponse.Id;
	}

	public async Task<Guid> CreateMessage(Guid scheduleId, bool withFiles = false)
	{
		var request = new CreateMessageRequest
		{
			ScheduleId = scheduleId,
			TimePosting = DateTimeOffset.UtcNow.AddDays(1),
			Files = withFiles ? [FileHelper.GetTestIFormFile()] : null
		};

		var response = await client.PostMultipartFormAsync<CreateMessageResponse>(Routes.Message.Root, request);
		return response.Id;
	}
}