using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public sealed class CreateScheduleRequest
{
	[Required]
	[MaxLength(100)]
	public required string Name { get; init; }

	[Required]
	[MaxLength(100)]
	public required string Channel { get; init; }

	[Required]
	public required Guid TelegramBotId { get; init; }
}