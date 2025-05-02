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