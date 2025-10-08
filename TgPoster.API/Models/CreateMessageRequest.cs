using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public sealed class CreateMessageRequest : IValidatableObject
{
    /// <summary>
    ///     Id расписания
    /// </summary>
    public required Guid ScheduleId { get; set; }

    /// <summary>
    ///     Время поста
    /// </summary>
    public required DateTimeOffset TimePosting { get; set; }

    /// <summary>
    ///     Текстовое сообщение
    /// </summary>
    public string? TextMessage { get; set; }

    /// <summary>
    ///     Файлы сообщения
    /// </summary>
    public List<IFormFile> Files { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationErrors = new List<ValidationResult>();
        if (DateTimeOffset.UtcNow > TimePosting)
        {
            validationErrors.Add(new ValidationResult(
                "Текущее время больше времени поста",
                [nameof(TimePosting)]
            ));
        }

        return validationErrors;
    }
}