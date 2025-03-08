using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public class CreateMessagesFromFilesRequest : IValidatableObject
{
    public required Guid ScheduleId { get; set; }
    public required List<IFormFile> Files { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationErrors = new List<ValidationResult>();

        if (!Files.Any())
        {
            validationErrors.Add(new ValidationResult("Files are required", new[] { nameof(Files) }));
        }

        return validationErrors;
    }
}