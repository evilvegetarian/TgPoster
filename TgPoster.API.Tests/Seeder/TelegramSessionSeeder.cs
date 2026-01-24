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
			Name = "TelegramBot",
			ApiId = "asdas1212",
			ApiHash = "asdas1212",
			PhoneNumber = "121241241",
			UserId = GlobalConst.Worked.UserId
		};

		await context.TelegramSessions.AddRangeAsync(session);
	}
}