using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.API.Tests.Helper;

internal class UserBuilder(PosterContext context)
{
	private static readonly Faker faker = new("ru");

	private readonly User user = new()
	{
		Id = Guid.NewGuid(),
		Email = new Email(faker.Internet.Email()),
		PasswordHash = faker.Random.Hash(),
		UserName = new UserName(faker.Internet.UserName())
	};

	private readonly List<User> users = [];

	public UserBuilder WithName(string value)
	{
		user.UserName = new UserName(value);
		return this;
	}

	public UserBuilder WithEmail(string value)
	{
		user.Email = new Email(value);
		return this;
	}

	public UserBuilder WithPasswordHash(string value)
	{
		user.PasswordHash = value;
		return this;
	}

	public User Build() => user;

	public List<User> BuildList(int count = 5)
	{
		for (var i = 0; i < count; i++)
		{
			users.Add(new UserBuilder(context).Build());
		}

		return users;
	}

	public List<User> CreateList()
	{
		context.Users.AddRange(users);
		context.SaveChanges();
		return users;
	}

	public User Create()
	{
		context.Users.AddRange(user);
		context.SaveChanges();
		return user;
	}

	public async Task<User> CreateAsync(CancellationToken ct = default)
	{
		await context.Users.AddRangeAsync(user);
		await context.SaveChangesAsync(ct);
		return user;
	}
}