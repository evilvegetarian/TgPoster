using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

/// <summary>
/// Ответ на создание Ютуб аккаунта
/// </summary>
public class CreateYouTubeAccountResponse
{
	public required string Url { get; set; }
}