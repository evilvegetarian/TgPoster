using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Data.Configurations;

internal class UserNameConverter : ValueConverter<UserName, string>
{
    public UserNameConverter(ConverterMappingHints? mappingHints = null)
        : base(
            name => name.Value,
            str => new UserName(str),
            mappingHints)
    {
    }
}