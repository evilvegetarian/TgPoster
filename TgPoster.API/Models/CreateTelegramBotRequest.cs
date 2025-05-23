using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public class CreateTelegramBotRequest : IValidatableObject
{
    public required string Token { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationErrors = new List<ValidationResult>();

        if (Token.Length < 1)
        {
            validationErrors.Add(new ValidationResult("Token are required", [nameof(Token)]));
        }

        return validationErrors;
    }
}