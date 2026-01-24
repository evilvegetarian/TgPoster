using MediatR;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public sealed record GetRepostSettingsQuery(Guid ScheduleId) : IRequest<CreateRepostSettingsResponse?>;
