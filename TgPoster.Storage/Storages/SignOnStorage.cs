using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Storages;

internal sealed class SignOnStorage(PosterContext context, GuidFactory guidFactory) : ISignOnStorage
{
	public async Task<Guid> CreateUserAsync(string username, string password, CancellationToken token)
	{
		var user = new User
		{
			Id = guidFactory.New(),
			UserName = new UserName(username),
			PasswordHash = password
		};

		await context.Users.AddAsync(user, token);
		await context.SaveChangesAsync(token);

		return user.Id;
	}

	public Task<bool> HaveUserNameAsync(string userName, CancellationToken token)
	{
		return context.Users.AnyAsync(x => x.UserName == new UserName(userName), token);
	}
}