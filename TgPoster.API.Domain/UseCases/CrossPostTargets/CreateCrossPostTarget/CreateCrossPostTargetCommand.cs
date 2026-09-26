using MediatR;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;

/// <summary>
///     Команда создания связки расписания с аккаунтом соцсети
/// </summary>
public sealed record CreateCrossPostTargetCommand(
	Guid ScheduleId,
	Guid SocialAccountId,
	CrossPostFormat Format,
	CrossPostLinkTarget LinkTarget,
	string? CustomLink,
	string? CallToAction,
	bool IncludeMedia,
	bool IncludeParsed,
	int DelayMinutes) : IRequest<CreateCrossPostTargetResponse>;
