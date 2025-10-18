using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TgPoster.Storage.Data.Configurations.ConfigurationConverters;

internal class StringListJsonConverter : ValueConverter<ICollection<string>, string>
{
    internal StringListJsonConverter(ConverterMappingHints? mappingHints = null)
        : base(
            ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<ICollection<string>>(json, (JsonSerializerOptions?)null)
                    ?? new List<string>(),
            mappingHints
        )
    {
    }
}