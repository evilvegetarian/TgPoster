using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Telegram.Bot.Exceptions;
using TgPoster.Exceptions;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Tests;

public sealed class ErrorHandlingEndpointShould(AuthorizationTestFixture fixture)
	: IClassFixture<AuthorizationTestFixture>
{
	[Theory]
	[InlineData("authorization", HttpStatusCode.Unauthorized)]
	[InlineData("argument", HttpStatusCode.BadRequest)]
	[InlineData("validation", HttpStatusCode.BadRequest)]
	[InlineData("missing", HttpStatusCode.NotFound)]
	public async Task ReturnProblemDetails_ForExpectedExceptions(string kind, HttpStatusCode status)
	{
		using var client = fixture.GetClient();

		var response = await client.GetAsync("/test/errors/" + kind);

		response.StatusCode.ShouldBe(status);
		var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
		problem.ShouldNotBeNull();
		problem.Status.ShouldBe((int)status);
		problem.Title.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task HideExceptionDetails_WhenUnexpectedErrorOccurs()
	{
		using var client = fixture.GetClient();

		var response = await client.GetAsync("/test/errors/unexpected");
		var content = await response.Content.ReadAsStringAsync();

		response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
		content.ShouldNotContain(ErrorProbeController.PrivateMessage);
		content.ShouldNotContain(nameof(InvalidOperationException));
		content.ShouldNotContain(nameof(ErrorProbeController));
		var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
		problem.ShouldNotBeNull();
		problem.Detail.ShouldBeNull();
	}

	[Theory]
	[InlineData("chat not found", "Канал не найден")]
	[InlineData("message to delete not found", "Сообщение для удаления не найдено")]
	[InlineData("message is not modified", "Сообщение не было изменено")]
	[InlineData("message can't be edited", "Этот тип сообщения")]
	[InlineData("bot was blocked by the user", "Бот был заблокирован")]
	[InlineData("bot is not a member of the channel chat", "Бот не является участником")]
	[InlineData("bot is not a member of the supergroup chat", "Бот не является участником")]
	[InlineData("user is deactivated", "Пользователь деактивировал")]
	[InlineData("wrong file identifier/HTTP URL specified", "Неверный идентификатор")]
	[InlineData("unknown telegram failure", "Произошла ошибка Telegram")]
	public async Task TranslateTelegramErrors(string error, string expected)
	{
		using var client = fixture.GetClient();

		var response = await client.GetAsync("/test/errors/telegram?message=" + Uri.EscapeDataString(error));

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
		problem.ShouldNotBeNull();
		problem.Title.ShouldNotBeNull();
		problem.Title.ShouldContain(expected);
	}
}

[ApiController]
[Route("test/errors")]
public sealed class ErrorProbeController : ControllerBase
{
	internal const string PrivateMessage = "private connection details must never leave the server";

	[HttpGet("{kind}")]
	public IActionResult Get(string kind, [FromQuery] string? message) => throw kind switch
	{
		"authorization" => new AuthorizationRequiredException(),
		"argument" => new ArgumentException("Invalid argument"),
		"validation" => new ValidationException("Invalid value"),
		"missing" => new ProxyNotFoundException(Guid.Empty),
		"telegram" => new ApiRequestException("Bad Request: " + message, 400),
		_ => new InvalidOperationException(PrivateMessage)
	};
}
