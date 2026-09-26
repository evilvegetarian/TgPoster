using MediatR;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;

/// <summary>
///     Команда обновления связки расписания с аккаунтом соцсети
/// </summary>
public sealed record UpdateCrossPostTargetCommand(
	Guid ScheduleId,
	Guid Id,
	bool IsActive,
	CrossPostFormat Format,
	CrossPostLinkTarget LinkTarget,
	string? CustomLink,
	string? CallToAction,
	bool IncludeMedia,
	bool IncludeParsed,
	int DelayMinutes) : IRequest;
