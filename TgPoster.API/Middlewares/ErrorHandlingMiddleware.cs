using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Shared.SharedException;
using Telegram.Bot.Exceptions;
using TgPoster.API.Domain.Exceptions;

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
				case ArgumentException or ValidationException or DomainException or SharedException:
					problemDetails = problemDetailsFactory.CreateProblemDetails(
						context,
						StatusCodes.Status400BadRequest,
						exception.Message);
					logger.LogInformation(exception, "Некорректный запрос от клиента.");
					break;

				case RequestException apiException:
					var userFriendlyMessage = TranslateTelegramError(apiException.Message);
					problemDetails = problemDetailsFactory.CreateProblemDetails(
						context,
						StatusCodes.Status400BadRequest,
						userFriendlyMessage);
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

	private string TranslateTelegramError(string telegramMessage)
	{
		var errorDescription = telegramMessage;
		var separatorIndex = telegramMessage.IndexOf(": ");
		if (separatorIndex > 0)
		{
			errorDescription = telegramMessage.Substring(separatorIndex + 2);
		}

		switch (errorDescription)
		{
			case "chat not found":
				return "Канал не найден. Возможно, вы указали неверно канал или бот был удален из чата.";

			case "message to delete not found":
				return "Сообщение для удаления не найдено.";

			case "message is not modified":
				return "Сообщение не было изменено. Вероятно, новое содержимое совпадает со старым.";

			case "message can't be edited":
				return "Этот тип сообщения не может быть отредактирован.";

			case "bot was blocked by the user":
				return "Бот был заблокирован пользователем.";

			case "bot is not a member of the channel chat":
			case "bot is not a member of the supergroup chat":
				return "Бот не является участником этого канала или группы.";

			case "user is deactivated":
				return "Пользователь деактивировал свой аккаунт.";

			case "wrong file identifier/HTTP URL specified":
				return "Неверный идентификатор файла или URL.";

			default:
				return $"Произошла ошибка Telegram: {errorDescription}";
		}
	}
}