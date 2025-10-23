namespace TgPoster.API.Models;

public sealed class ApproveMessagesRequest
{
	public List<Guid> MessagesIds { get; set; } = [];
}