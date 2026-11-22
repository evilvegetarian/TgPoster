using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class InvalidDiscoverSelectionException()
	: DomainException("Обновите список похожих площадок и выберите каналы из рекомендаций Telegram");
