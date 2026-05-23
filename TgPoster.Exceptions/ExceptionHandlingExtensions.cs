using Microsoft.AspNetCore.Builder;
using TgPoster.Exceptions.Middlewares;

namespace TgPoster.Exceptions;

public static class ExceptionHandlingExtensions
{
    /// <summary>
    ///     Подключает глобальный обработчик исключений TgPoster
    /// </summary>
    /// <param name="app">Конвейер обработки HTTP-запросов</param>
    /// <returns>Тот же конвейер для чейнинга</returns>
    public static IApplicationBuilder UseTgPosterExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
