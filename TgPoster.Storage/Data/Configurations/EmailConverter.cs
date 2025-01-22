using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TgPoster.Storage.VO;

namespace TgPoster.Storage.Data.Configurations;

public class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter(ConverterMappingHints? mappingHints = null)
        : base(
            email => email.Value,
            str => new Email(str),
            mappingHints) { }
}