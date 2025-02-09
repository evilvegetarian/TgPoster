namespace TgPoster.Storage.Data.Entities;

public sealed class Day : BaseEntity
{
    /// <summary>
    ///     Id расписания.
    /// </summary>
    public required Guid ScheduleId { get; set; }

    /// <summary>
    ///     День недели постинга.
    /// </summary>
    public required DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    ///     Время постинга.
    /// </summary>
    public ICollection<TimeOnly> TimePostings { get; set; } = [];

    /// <summary>
    ///     Расписание.
    /// </summary>
    public Schedule Schedule { get; set; } = null!;
}