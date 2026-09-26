using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;

/// <summary>
///     Ответ с настройками связки расписания и аккаунта соцсети
/// </summary>
public sealed record CrossPostTargetResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required Guid SocialAccountId { get; init; }
	public required SocialPlatform Platform { get; init; }
	public required string AccountName { get; init; }
	public required SocialAccountStatus AccountStatus { get; init; }
	public required bool IsActive { get; init; }
	public required CrossPostFormat Format { get; init; }
	public required CrossPostLinkTarget LinkTarget { get; init; }
	public string? CustomLink { get; init; }
	public string? CallToAction { get; init; }
	public required bool IncludeMedia { get; init; }
	public required bool IncludeParsed { get; init; }
	public required int DelayMinutes { get; init; }
}
