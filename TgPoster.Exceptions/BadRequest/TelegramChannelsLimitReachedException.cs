using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Аккаунт уже состоит в максимальном числе каналов и групп (CHANNELS_TOO_MUCH), вступить в новый нельзя
/// </summary>
public sealed class TelegramChannelsLimitReachedException()
	: DomainException("Аккаунт уже состоит в максимальном числе каналов и групп "
	                  + "(500, с Telegram Premium — 1000). Выйдите из ненужных и повторите");
