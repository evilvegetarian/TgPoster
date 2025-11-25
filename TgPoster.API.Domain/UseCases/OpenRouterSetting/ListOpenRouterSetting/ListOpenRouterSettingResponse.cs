using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public class ListOpenRouterSettingResponse
{
	[Required]
	public required List<OpenRouterSettingResponse> OpenRouterSettingResponses { get; set; } = [];
}