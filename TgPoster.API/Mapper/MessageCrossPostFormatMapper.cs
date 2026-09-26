using Shared.Enums;

namespace TgPoster.API.Mapper;

/// <summary>
///     Маппер формата кросс-поста из запроса в формат хранения
/// </summary>
internal static class MessageCrossPostFormatMapper
{
	/// <summary>
	///     Преобразовать формат кросс-поста запроса в формат хранения
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static CrossPostFormat? ToCrossPostFormat(this MessageCrossPostFormat? value)
	{
		return value is null or MessageCrossPostFormat.Inherit
			? null
			: (CrossPostFormat)(int)value.Value;
	}
}
