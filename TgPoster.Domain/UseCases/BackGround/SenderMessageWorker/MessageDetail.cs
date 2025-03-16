namespace TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;

public class MessageDetail
{
    public required string Api { get; set; }
    public required long ChannelId { get; set; }
    public List<MessageDto> MessageDto { get; set; } = [];
}