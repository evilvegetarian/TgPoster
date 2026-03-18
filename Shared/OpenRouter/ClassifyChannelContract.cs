namespace Shared.OpenRouter;

/// <summary>
///     Контракт MassTransit для классификации тематики канала через LLM.
/// </summary>
public sealed class ClassifyChannelContract
{
	public required Guid ChannelId { get; init; }
}
