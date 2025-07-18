namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

public class ParseChannelsResponse
{
    public required Guid Id { get; set; }
    public required Guid ScheduleId { get; set; }
    public required bool DeleteText { get; set; }
    public required bool DeleteMedia { get; set; }
    public required string[] AvoidWords { get; set; } = [];
    public required bool NeedVerifiedPosts { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Тестовый статус парсинга
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Работает ли парсинг
    /// </summary>
    public required bool IsActive { get; set; }
}