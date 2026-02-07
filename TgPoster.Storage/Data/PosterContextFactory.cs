using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Security.IdentityServices;

namespace TgPoster.Storage.Data;

internal sealed class PosterContextFactory : IDesignTimeDbContextFactory<PosterContext>
{
	public PosterContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("appsettings.Development.json", optional: true)
			.AddUserSecrets("8cad5989-a758-42b6-a3fd-da884daf90bb")
			.Build();

		var connectionString = configuration["DataBase:ConnectionString"];

		var options = new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(connectionString)
			.Options;

		return new PosterContext(options, new DesignTimeIdentityProvider());
	}
}

file sealed class DesignTimeIdentityProvider : IIdentityProvider
{
	public Identity Current => Identity.Anonymous;
	public void Set(Identity identity) { }
}
