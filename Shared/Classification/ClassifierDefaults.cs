namespace Shared.Classification;

/// <summary>
///     Значения настроек LLM-классификатора каналов по умолчанию: ими засевается запись в БД,
///     и к ним можно вернуться из интерфейса
/// </summary>
public static class ClassifierDefaults
{
	/// <summary>
	///     Место в системном промпте, куда подставляется список тематик через запятую
	/// </summary>
	public const string CategoriesPlaceholder = "{categories}";

	/// <summary>Модель OpenRouter</summary>
	public const string Model = "qwen/qwen3-vl-8b-instruct";

	/// <summary>Включён ли классификатор</summary>
	public const bool IsEnabled = true;

	/// <summary>Сколько каналов классифицировать за один запуск</summary>
	public const int BatchSize = 2;

	/// <summary>Как часто запускать классификатор, в минутах</summary>
	public const int IntervalMinutes = 20;

	/// <summary>Сколько последних постов канала брать в выборку</summary>
	public const int MessageSampleCount = 25;

	/// <summary>Сколько фото из постов отправлять в модель (0 — не отправлять)</summary>
	public const int PhotoCount = 6;

	/// <summary>Тематики, из которых модель выбирает ровно одну</summary>
	public static IReadOnlyList<string> Categories { get; } =
	[
		"Технологии", "Новости", "Крипто", "Бизнес", "Маркетинг", "Развлечения", "Образование",
		"Политика", "Спорт", "Здоровье", "Путешествия", "Еда", "Музыка", "Игры", "Авто",
		"Финансы", "Наука", "Дизайн", "Юмор", "18+", "Другое"
	];

	/// <summary>
	///     Системный промпт; <see cref="CategoriesPlaceholder" /> заменяется списком тематик
	/// </summary>
	public const string SystemPrompt = """
	                                   Ты — классификатор Telegram-каналов. На вход получаешь название, описание,
	                                   последние посты и, если есть, фотографии из этих постов. Используй И текст,
	                                   И визуальный контекст изображений для определения тематики.

	                                   Категории (выбери РОВНО ОДНУ): {categories}.

	                                   Правила:
	                                   - Если данных мало или они противоречивы — ставь confidence < 0.5 и категорию "Другое".
	                                   - tags: 3-5 коротких тегов (1-2 слова каждый), отражающих узкую специфику.
	                                   - language: ISO-код основного языка постов (ru, en, uk, ...).
	                                   - Отвечай СТРОГО валидным JSON без markdown-обёрток, без ```, без комментариев.

	                                   Формат ответа:
	                                   {"category":"...","subcategory":"...","tags":["...","..."],"language":"...","confidence":0.0-1.0}
	                                   """;
}
