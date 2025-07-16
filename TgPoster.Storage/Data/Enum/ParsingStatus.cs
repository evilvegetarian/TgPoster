namespace TgPoster.Storage.Data.Enum;

public enum ParsingStatus
{
    New = 0,
    InHandle = 1,
    Canceled = 2,
    Waiting = 3,
    Finished = 4,
    Failed = 100
}

public static class ParsingStatusExtensions
{
    public static string GetStatus(this ParsingStatus status)
    {
        return status switch
        {
            ParsingStatus.New => "Новый запрос парсинга",
            ParsingStatus.InHandle => "В обработке",
            ParsingStatus.Canceled => "Отменено",
            ParsingStatus.Waiting => "Ожидает",
            ParsingStatus.Finished => "Закончен парсинг",
            ParsingStatus.Failed => "Ошибка",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}