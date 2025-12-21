using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

public record LoginYouTubeCommand(IFormFile? JsonFile, string? ClientId, string? ClientSecret, string RedirectUrl)
	: IRequest<string>;