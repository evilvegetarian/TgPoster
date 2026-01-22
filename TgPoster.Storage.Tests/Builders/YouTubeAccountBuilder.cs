using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public class YouTubeAccountBuilder(PosterContext context)
{
	private readonly YouTubeAccount youtube = new()
	{
		Name = "asdasd",
		AccessToken = "dsadasfqw",
		ClientId = "afasf13312",
		ClientSecret = "afasfasfasf",
		UserId = new UserBuilder(context).Create().Id,
		Id = Guid.NewGuid()
	};

	public YouTubeAccountBuilder WithUser(User user)
	{
		youtube.User = user;
		youtube.UserId = user.Id;
		return this;
	}

	public YouTubeAccount Create()
	{
		context.YouTubeAccounts.AddRange(youtube);
		context.SaveChanges();
		return youtube;
	}
}