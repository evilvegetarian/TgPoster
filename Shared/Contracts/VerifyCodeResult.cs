namespace Shared.Contracts;

/// <summary>
///     Результат верификации кода авторизации.
/// </summary>
public sealed class VerifyCodeResult
{
	public bool Success { get; init; }
	public bool RequiresPassword { get; init; }
	public string? Message { get; init; }
}
