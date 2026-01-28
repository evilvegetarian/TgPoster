using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;
using TgPoster.API.Domain.UseCases.Days.GetDays;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public class DayEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.Day.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly CreateHelper helper = new(fixture.AuthClient);

	[Fact]
	public async Task GetDayOfWeek_ValidData_ShouldReturnOk()
	{
		var response = await client.GetAsync(Routes.Day.DayOfWeek);
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var result = await response.Content.ReadFromJsonAsync<DayOfWeekListResponse>();
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeEmpty();
		result.Items.Count.ShouldBe(7);
	}

	[Fact]
	public async Task Get_WithAnotherUser_ShouldReturnNotFound()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(17, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 5
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var responseAnother = await anotherClient.GetAsync(Url + "?scheduleId=" + scheduleId);
		responseAnother.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Get_WithValidData_ShouldOk()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(17, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 5
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var getResponse = await client.GetAsync<DayListResponse>(Url + "?scheduleId=" + scheduleId);
		getResponse.Items.ShouldContain(x => x.ScheduleId == scheduleId);
		getResponse.Items.ShouldContain(x => request.DaysOfWeek.Select(day => day.DayOfWeekPosting).Contains(x.DayOfWeek));
	}

	[Fact]
	public async Task Create_NonExistScheduleId_ShouldReturnOk()
	{
		var request = new CreateDaysRequest
		{
			ScheduleId = Guid.NewGuid(),
			DaysOfWeek = []
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Create_DoubleDayOfWeek_ShouldReturnBadRequest()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(10, 15),
					EndPosting = new TimeOnly(20, 30),
					Interval = 45
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var response2 = await client.PostAsync(Url, request.ToStringContent());
		response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithValidData_ShouldReturnOk()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(12, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 10
				},
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Thursday,
					StartPosting = new TimeOnly(14, 25),
					EndPosting = new TimeOnly(18, 00),
					Interval = 14
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var getResponse = await client.GetAsync<DayListResponse>(Url + "?scheduleId=" + scheduleId);
		getResponse.Items.Count.ShouldBe(2);
	}

	[Fact]
	public async Task Create_WithDuplicatDayOfWeek_ShouldReturnBadRequest()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(12, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 10
				},
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(14, 25),
					EndPosting = new TimeOnly(18, 00),
					Interval = 14
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithEndPostingMoreStartPosting_ShouldReturnBadRequest()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(18, 55),
					EndPosting = new TimeOnly(18, 00),
					Interval = 10
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithIntervalMoreTiming_ShouldReturnBadRequest()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(17, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 90
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithNonExistScheduleId_ShouldReturnNotFound()
	{
		var request = new CreateDaysRequest
		{
			ScheduleId = Guid.Parse("2feb5ba7-8c89-46e2-8b53-8776fa7e0647"),
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(17, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 5
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Create_WithAnotherUser_ShouldReturnNotFound()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(15, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 5
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var responseAnother = await anotherClient.PostAsync(Url, request.ToStringContent());
		responseAnother.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateTimeDay_WithNonExistScheduleId_ShouldReturnNotFound()
	{
		var nonExistScheduleId = Guid.Parse("4b44be73-3b59-41e2-baff-6b7857c2be6f");
		var upd = new UpdateTimeRequest
		{
			ScheduleId = nonExistScheduleId,
			DayOfWeek = DayOfWeek.Monday,
			Times =
			[
				new TimeOnly(10, 15),
				new TimeOnly(20, 30),
				new TimeOnly(20, 40)
			]
		};
		var response = await client.PatchAsync(Url + "/time", upd.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateTimeDay_WithDuplicateTime_ShouldReturnBadRequest()
	{
		var upd = new UpdateTimeRequest
		{
			ScheduleId = Guid.Parse("3cce60f0-56bd-4e72-93ab-1239102e85e1"),
			DayOfWeek = DayOfWeek.Monday,
			Times =
			[
				new TimeOnly(10, 15),
				new TimeOnly(10, 15),
				new TimeOnly(20, 15),
				new TimeOnly(20, 15),
				new TimeOnly(20, 40),
				new TimeOnly(21, 46),
				new TimeOnly(22, 40)
			]
		};
		var response = await client.PatchAsync(Url + "/time", upd.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateTimeDay_ValidData_ShouldReturnOk()
	{
		var scheduleId = await helper.CreateSchedule();
		await helper.CreateDay(scheduleId, DayOfWeek.Monday);
		var upd = new UpdateTimeRequest
		{
			ScheduleId = scheduleId,
			DayOfWeek = DayOfWeek.Monday,
			Times =
			[
				new TimeOnly(10, 15),
				new TimeOnly(20, 15),
				new TimeOnly(21, 46),
				new TimeOnly(22, 40)
			]
		};
		var response = await client.PatchAsync(Url + "/time", upd.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		var getResponse = await client.GetAsync<DayListResponse>(Url + "?scheduleId=" + scheduleId);
		getResponse.Items.ShouldContain(x => x.DayOfWeek == upd.DayOfWeek);
	}

	[Fact]
	public async Task UpdateTimeDay_WithAnotherUser_ShouldReturnNotFound()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreateDaysRequest
		{
			ScheduleId = scheduleId,
			DaysOfWeek =
			[
				new DayOfWeekRequest
				{
					DayOfWeekPosting = DayOfWeek.Monday,
					StartPosting = new TimeOnly(17, 10),
					EndPosting = new TimeOnly(18, 00),
					Interval = 5
				}
			]
		};
		var response = await client.PostAsync(Url, request.ToStringContent());
		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var responseAnother = await anotherClient.PostAsync(Url, request.ToStringContent());
		responseAnother.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}