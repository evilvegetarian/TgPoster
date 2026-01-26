using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;

public sealed record ListRepostSettingsQuery : IRequest<ListRepostSettingsResponse>;
