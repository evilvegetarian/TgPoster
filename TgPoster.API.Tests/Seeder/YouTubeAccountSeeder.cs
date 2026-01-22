using Microsoft.EntityFrameworkCore;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.API.Tests.Seeder;

internal sealed class YouTubeAccountSeeder(PosterContext context) : BaseSeeder
{
	public override async Task Seed()
	{
		if (await context.YouTubeAccounts.AnyAsync())
		{
			return;
		}

		var youtubeAccount = new YouTubeAccount
		{
			Id = GlobalConst.YouTubeAccountId,
			Name = "Test YouTube Account",
			AccessToken = "test_access_token",
			ClientId = "test_client_id",
			ClientSecret = "test_client_secret",
			UserId = GlobalConst.Worked.UserId
		};

		await context.YouTubeAccounts.AddAsync(youtubeAccount);
	}
}