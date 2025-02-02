using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Data.Configurations;

internal class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter(ConverterMappingHints? mappingHints = null)
        : base(
            email => email.Value,
            str => new Email(str),
            mappingHints) { }
}