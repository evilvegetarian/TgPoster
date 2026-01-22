using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Shared.Services;

public static class Configuration
{
	public static void ConfigureMassTransient(
		this IBusRegistrationConfigurator busRegistrationConfigurator,
		string connectionString
	)
	{
		var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

		busRegistrationConfigurator.AddOptions<SqlTransportOptions>().Configure(options =>
		{
			options.Host = connectionBuilder.Host;
			options.Database = connectionBuilder.Database;
			options.Schema = "messaging";
			options.Role = "messaging";
			options.Username = connectionBuilder.Username;
			options.Password = connectionBuilder.Password;
			options.AdminUsername = connectionBuilder.Username;
			options.AdminPassword = connectionBuilder.Password;
		});
	}
}