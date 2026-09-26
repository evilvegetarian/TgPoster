using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed record MessageResponse
{
	public required Guid Id { get; init; }
	public string? TextMessage { get; init; }
	public required DateTimeOffset TimePosting { get; init; }
	public required Guid ScheduleId { get; init; }
	public required bool NeedApprove { get; init; }
	public required bool CanApprove { get; init; }
	public List<FileResponse> Files { get; init; } = [];
	public required bool IsSent { get; init; }
	public required bool HasVideo { get; init; }
	public required bool HasYouTubeAccount { get; init; }

	/// <summary>
	///     Участвует ли пост в кросс-постинге
	/// </summary>
	public required bool CrossPostEnabled { get; init; }

	/// <summary>
	///     Переопределение формата кросс-поста, null — как в связке
	/// </summary>
	public CrossPostFormat? CrossPostFormat { get; init; }

	/// <summary>
	///     Статусы публикации поста в соцсетях
	/// </summary>
	public List<CrossPostStatusResponse> CrossPosts { get; init; } = [];
}