namespace TgPoster.Storage.Data.Enum;

public enum MessageStatus
{
	Register = 0,
	InHandle = 1,
	Send = 2,
	Error = 10,
	Cancel = 20,
}

public static class MessageStatusExtensions
{
	public static List<MessageStatus> GetBadStatus()
	{
		return [MessageStatus.Cancel, MessageStatus.Error];
	}
}