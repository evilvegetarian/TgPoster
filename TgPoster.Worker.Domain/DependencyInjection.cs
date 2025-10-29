using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Contracts;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Worker.Domain;

public static class DependencyInjection
{
	public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
	{
		var telegramOptions = configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
		services.AddSingleton(telegramOptions);

		services.AddTelegramSession(configuration);

		services.AddMassTransient(configuration);

		services.AddHangfire(cfg =>
		{
			cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseMemoryStorage();
		});
		services.AddHangfireServer();
		services.AddScoped<SenderMessageWorker>();
		services.AddScoped<ParseChannelWorker>();
		services.AddScoped<ParseChannelUseCase>();
		services.AddScoped<TimePostingService>();
		services.AddScoped<VideoService>();
		services.AddScoped<TelegramExecuteServices>();

		return services;
	}

	private static void AddTelegramSession(this IServiceCollection services, IConfiguration configuration)
	{
		var telegramSettings = configuration.GetSection(nameof(TelegramSettings)).Get<TelegramSettings>()!;
		services.AddSingleton(telegramSettings);

		services.AddSingleton(_ =>
		{
			string? Config(string key) => key switch
			{
				"api_id" => telegramSettings.api_id,
				"api_hash" => telegramSettings.api_hash,
				"phone_number" => telegramSettings.phone_number,
				_ => null
			};

			var client = new WTelegram.Client(Config);

			Console.WriteLine("Logging in to Telegram...");
			client.LoginUserIfNeeded().GetAwaiter().GetResult();
			Console.WriteLine("Login successful. Application is starting.");

			return client;
		});
	}

	private static void AddMassTransient(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddMassTransit(x =>
		{
			x.AddConsumer<ParseChannelConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			x.AddConsumer<ProcessMessageConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			const string queueName = "process-message-queue";

			x.UsingPostgres((context, cfg) =>
			{
				cfg.ConfigureEndpoints(context);
				cfg.ReceiveEndpoint(queueName, e =>
				{
					var partition = e.CreatePartitioner(50);
					e.ConfigureConsumer<ProcessMessageConsumer>(context, c =>
					{
						c.Message<ProcessMessage>(m =>
							m.UsePartitioner(partition, p => p.Message.TelegramBotId));
					});
				});
			});
			var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
			x.ConfigureMassTransient(dataBase.ConnectionString);
			x.AddPostgresMigrationHostedService();
		});
	}

	public static void AddHangfire(this WebApplication app)
	{
		app.UseHangfireDashboard("/hangfire", new DashboardOptions
		{
			Authorization = [new AllowAllAuthorizationFilter()]
		});

		using var scope = app.Services.CreateScope();
		var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

		recurringJobManager.AddOrUpdate<SenderMessageWorker>(
			"process-sender-message-job",
			worker => worker.ProcessMessagesAsync(),
			Cron.Minutely());

		recurringJobManager.AddOrUpdate<ParseChannelWorker>(
			"process-parse-channel-job",
			worker => worker.ProcessMessagesAsync(),
			Cron.Daily());
	}

	public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context)
		{
			return true;
		}
	}
}