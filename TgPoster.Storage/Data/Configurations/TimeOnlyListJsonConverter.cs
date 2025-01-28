using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TgPoster.Storage.Data.Configurations;

internal class TimeOnlyListJsonConverter : ValueConverter<IEnumerable<TimeOnly>, string>
{
    public TimeOnlyListJsonConverter(ConverterMappingHints? mappingHints = null)
        : base(
            times => JsonSerializer.Serialize(times, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<TimeOnly>>(json, (JsonSerializerOptions?)null) ?? new List<TimeOnly>(),
            mappingHints
        ) { }
}