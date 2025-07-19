using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public class CreateParseChannelRequest : IValidatableObject
{
    /// <summary>
    ///     Канал который парсят.
    /// </summary>
    public required string Channel { get; set; }

    /// <summary>
    ///     Раз в день будет проверять новые посты.
    /// </summary>
    public required bool AlwaysCheckNewPosts { get; set; }

    /// <summary>
    ///     Расписание для которого парсят канал.
    /// </summary>
    public required Guid ScheduleId { get; set; }

    /// <summary>
    ///     Нужно ли удалять текст, и оставлять только картинку.
    /// </summary>
    public bool DeleteText { get; set; }

    /// <summary>
    ///     Нужно ли удалять медиа, и оставлять только текст.
    /// </summary>
    public bool DeleteMedia { get; set; }

    /// <summary>
    ///     Избегать постов с данными словами, предложениями, словосочетаниями.
    /// </summary>
    public string[] AvoidWords { get; set; } = [];

    /// <summary>
    ///     Нужно ли подтверждать пост перед тем как его запостить в вашем канале.
    ///     Если не нужно, он будет автоматически поставлен в свободные даты {ScheduleId}.
    ///     Если расписание пусто, то нужно будет дополнительно подтверждать все равно.
    /// </summary>
    public bool NeedVerifiedPosts { get; set; }

    /// <summary>
    ///     Дата откуда парсить
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    ///     До какой даты парсить
    /// </summary>
    public DateTime? DateTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        if (DeleteMedia && DeleteText)
        {
            validationResults.Add(new ValidationResult("Нельзя одновременно удалить и текст и медиа",
                [nameof(DeleteText), nameof(DeleteMedia)]));
        }

        var today = DateTime.UtcNow.Date;

        if (DateFrom.HasValue && DateFrom.Value > today)
        {
            validationResults.Add(new ValidationResult("Дата начала парсинга не может быть в будущем",
                [nameof(DateFrom)]));
        }

        if (DateFrom.HasValue && DateTo.HasValue)
        {
            if (DateFrom.Value > DateTo.Value)
            {
                validationResults.Add(new ValidationResult(
                    "Дата начала парсинга должна быть меньше или равна дате окончания",
                    [nameof(DateFrom), nameof(DateTo)]));
            }
        }

        if (ScheduleId == Guid.Empty)
        {
            validationResults.Add(new ValidationResult("Необходимо указать ScheduleId", [nameof(ScheduleId)]));
        }

        if (string.IsNullOrWhiteSpace(Channel))
        {
            validationResults.Add(new ValidationResult("Имя канала не должно быть пустым", [nameof(Channel)]));
        }

        return validationResults;
    }
}