namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

public class ParseChannelsResponse
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public bool DeleteText { get; set; }
    public bool DeleteMedia { get; set; }
    public string[] AvoidWords { get; set; } = [];
    public bool NeedVerifiedPosts { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Тестовый статус парсинга
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Работает ли парсинг
    /// </summary>
    public bool IsActive { get; set; }
}