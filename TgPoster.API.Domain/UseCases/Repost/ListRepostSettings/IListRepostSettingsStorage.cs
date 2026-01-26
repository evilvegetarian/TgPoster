namespace TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;

public interface IListRepostSettingsStorage
{
	Task<List<RepostSettingsItemDto>> GetListAsync(Guid userId, CancellationToken ct);
}
