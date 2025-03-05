using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TgPoster.Storage.Data.Configurations;

internal class StringListJsonConverter : ValueConverter<ICollection<string>, string>
{
    public StringListJsonConverter(ConverterMappingHints? mappingHints = null)
        : base(
            ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<ICollection<string>>(json, (JsonSerializerOptions?)null)
                    ?? new List<string>(),
            mappingHints
        ) { }
}