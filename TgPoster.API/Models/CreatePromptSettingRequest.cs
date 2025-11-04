using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Request Соз
/// </summary>
public class CreateOpenRouterSettingRequest
{
	/// <summary>
	/// Модель в Open Router
	/// </summary>
	[MinLength(3)]
	public required string Model { get; set; }

	/// <summary>
	/// Токен для авторизации Open Router
	/// </summary>
	[MinLength(5)]
	public required string Token { get; set; }
}