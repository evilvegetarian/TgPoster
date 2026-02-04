namespace TgPoster.API.Models;

/// <summary>
/// Запрос на верификацию кода подтверждения
/// </summary>
/// <param name="Code">Код подтверждения</param>
public sealed record VerifyCodeRequest(string Code);