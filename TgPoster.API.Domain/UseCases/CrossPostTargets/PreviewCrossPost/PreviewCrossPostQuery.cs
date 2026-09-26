using MediatR;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Запрос предпросмотра кросс-поста для связок расписания
/// </summary>
public sealed record PreviewCrossPostQuery(
	Guid ScheduleId,
	Guid? SocialAccountId,
	CrossPostFormat? Format,
	CrossPostLinkTarget? LinkTarget,
	string? CustomLink,
	string? CallToAction,
	string? Text) : IRequest<List<CrossPostPreviewResponse>>;
