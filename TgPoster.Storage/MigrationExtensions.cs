using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TgPoster.Storage.Data;

namespace TgPoster.Storage;

/// <summary>
///     Расширения для применения миграций базы данных
/// </summary>
public static class MigrationExtensions
{
	/// <summary>
	///     Применяет незавершённые миграции базы данных при старте приложения
	/// </summary>
	/// <param name="services">Провайдер сервисов приложения</param>
	/// <param name="ct">Токен отмены</param>
	public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
	{
		await using var scope = services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<PosterContext>();
		await context.Database.MigrateAsync(ct);
	}
}
