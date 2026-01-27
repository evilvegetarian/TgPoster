namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public interface IGetRepostSettingsStorage
{
	Task<RepostSettingsResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct);
}
