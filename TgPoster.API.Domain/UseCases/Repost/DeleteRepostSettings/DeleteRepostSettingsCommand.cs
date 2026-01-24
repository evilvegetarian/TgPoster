using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

public sealed record DeleteRepostSettingsCommand(Guid Id) : IRequest<Unit>;
