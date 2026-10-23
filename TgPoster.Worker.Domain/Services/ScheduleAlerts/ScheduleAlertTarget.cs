namespace TgPoster.Worker.Domain.Services.ScheduleAlerts;

/// <summary>
/// Получатель оповещения по расписанию: бот расписания и личный чат владельца с ним
/// </summary>
/// <param name="EncryptedBotToken"></param>
/// <param name="OwnerChatId"></param>
/// <param name="ScheduleName"></param>
/// <param name="ChannelName"></param>
public sealed record ScheduleAlertTarget(
	string EncryptedBotToken,
	long OwnerChatId,
	string ScheduleName,
	string ChannelName);
