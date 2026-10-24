using System.Diagnostics.CodeAnalysis;

namespace TgPoster.API.Domain.Services;

/// <summary>
/// Результат обращения к внешней системе, которая сама ловит свои ожидаемые ошибки
/// </summary>
public sealed class ApiResult
{
	private ApiResult(bool isSuccess, string? error)
	{
		IsSuccess = isSuccess;
		Error = error;
	}

	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	public string? Error { get; }

	public static ApiResult Success() => new(true, null);

	public static ApiResult Failure(string error) => new(false, error);
}
