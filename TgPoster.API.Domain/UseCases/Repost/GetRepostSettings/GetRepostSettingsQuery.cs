using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public sealed record GetRepostSettingsQuery(Guid ScheduleId) : IRequest<GetRepostSettingsResponse>;
