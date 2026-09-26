using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

public sealed record CreateMessageCommand(
	Guid ScheduleId,
	DateTimeOffset TimePosting,
	string? Text,
	List<IFormFile> Files,
	bool CrossPostEnabled = true,
	CrossPostFormat? CrossPostFormat = null
) : IRequest<CreateMessageResponse>;