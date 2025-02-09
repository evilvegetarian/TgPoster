using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Telegram.Bot.Exceptions;
using TgPoster.Domain.Exceptions;

namespace TgPoster.API.Middlewares;

internal sealed class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ProblemDetailsFactory problemDetailsFactory,
        ILogger<ErrorHandlingMiddleware> logger
    )
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error has happened with {RequestPath}, the message is {ErrorMessage}",
                context.Request.Path.Value,
                exception.Message);

            ProblemDetails problemDetails;
            switch (exception)
            {
                case ArgumentException or ValidationException or DomainException or ApiRequestException:
                    problemDetails = problemDetailsFactory.CreateProblemDetails(
                        context,
                        StatusCodes.Status400BadRequest,
                        exception.Message);
                    logger.LogInformation(exception, "Некорректный запрос от клиента.");
                    break;

                case NotFoundException:
                    problemDetails = problemDetailsFactory.CreateProblemDetails(
                        context,
                        StatusCodes.Status404NotFound,
                        exception.Message);
                    logger.LogInformation(exception, "Ресурса нет");
                    break;

                default:
                    problemDetails = problemDetailsFactory.CreateProblemDetails(
                        context,
                        StatusCodes.Status500InternalServerError,
                        "Unhandled error! Please contact us.");
                    logger.LogError(exception, "Unhandled exception occured");
                    break;
            }

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType());
        }
    }
}