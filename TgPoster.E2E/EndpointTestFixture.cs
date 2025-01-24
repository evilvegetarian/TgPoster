using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TgPoster.Storage.Config;

namespace TgPoster.E2E;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>($"{nameof(DataBase)}:{nameof(DataBase.ConnectionString)}",
                    dbContainer.GetConnectionString())
            }!)
            .Build();
        builder.UseConfiguration(configuration);
        builder.ConfigureLogging(cfg => cfg.ClearProviders());
        base.ConfigureWebHost(builder);
    }

    public async Task InitializeAsync()
    {
        await dbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContainer.DisposeAsync();
    }
}