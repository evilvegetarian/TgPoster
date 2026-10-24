using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStatus;

public interface IGetParseChannelStatusStorage
{
	/// <summary>
	/// Получить состояние задачи парсинга каналов; null, если воркер её ещё не регистрировал
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<WorkerJobStateDto?> GetParseJobStateAsync(CancellationToken ct);
}
