using Microsoft.EntityFrameworkCore;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.API.Tests.Seeder;

internal class TelegramSessionSeeder(PosterContext context) : BaseSeeder
{
	public override async Task Seed()
	{
		if (await context.TelegramSessions.AnyAsync())
		{
			return;
		}

		var session = new TelegramSession
		{
			Id = GlobalConst.Worked.TelegramSessionId,
			Name = "test-session",
			ApiId = "123",
			ApiHash = "test-api-hash",
			PhoneNumber = "+10000000000",
			UserId = GlobalConst.Worked.UserId
		};

		await context.TelegramSessions.AddRangeAsync(session);
	}
}