using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Data.Configurations;

internal class EmailConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<Email, string>(
    email => email.Value,
    str => new Email(str),
    mappingHints);