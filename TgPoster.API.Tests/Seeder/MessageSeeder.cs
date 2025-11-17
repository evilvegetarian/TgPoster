using TgPoster.Endpoint.Tests.Helper;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class MessageSeeder(PosterContext context) : BaseSeeder
{
	public override async Task Seed()
	{
		var messages = new List<Message>
		{
			new()
			{
				Id = Guid.Parse("8e5aa9af-4f23-4214-b396-9610e61467b5"),
				ScheduleId = GlobalConst.ScheduleId,
				IsTextMessage = false,
				TimePosting = new DateTime(2021, 12, 10, 12, 1, 2, DateTimeKind.Utc)
			},
			new()
			{
				Id = Guid.Parse("31716e56-3ec5-480c-ab6c-00032c76c29b"),
				ScheduleId = GlobalConst.ScheduleId,
				IsTextMessage = false,
				TimePosting = new DateTime(2025, 12, 10, 12, 1, 2, DateTimeKind.Utc)
			},
			new()
			{
				Id = Guid.Parse("052c0991-b6bc-4dd7-814a-5abe04fe58d0"),
				ScheduleId = GlobalConst.ScheduleId,
				IsTextMessage = true,
				TimePosting = new DateTime(2025, 12, 10, 14, 1, 2, DateTimeKind.Utc)
			},
			new()
			{
				Id = GlobalConst.MessageId,
				ScheduleId = GlobalConst.ScheduleId,
				IsTextMessage = true,
				TimePosting = new DateTime(2025, 10, 10, 14, 1, 2, DateTimeKind.Utc)
			}
		};
		await context.Messages.AddRangeAsync(messages);
	}
}