using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public sealed class UpdateTimeRequest : IValidatableObject
{
    public required Guid Id { get; set; }
    public List<TimeOnly> Times { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationErrors = new List<ValidationResult>();

        var duplicateTime = Times
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTime.Any())
        {
            var duplicateDaysNames = duplicateTime
                .Select(time => time.ToString())
                .ToArray();

            var errorMessage = $"Дублирующееся время: {string.Join(", ", duplicateDaysNames)}.";

            validationErrors.Add(new ValidationResult(
                errorMessage,
                [nameof(List<TimeOnly>)]
            ));
        }

        return validationErrors;
    }
}