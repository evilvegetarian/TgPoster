namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public interface IParseChannelRunStorage
{
	/// <summary>
	/// Записать проход источника в журнал парсинга
	/// </summary>
	/// <param name="report"></param>
	/// <param name="ct"></param>
	Task AddRunAsync(ParseChannelRunReport report, CancellationToken ct);
}
