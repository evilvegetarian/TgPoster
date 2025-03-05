using TgPoster.Storage.Data.Enum;
using DomainContentType = TgPoster.Domain.Services.ContentTypes;

namespace TgPoster.Storage.Mapping;

public static class ContentTypeMapper
{
    public static DomainContentType ToDomain(this ContentTypes storageType)
    {
        return storageType switch
        {
            ContentTypes.Photo => DomainContentType.Photo,
            ContentTypes.Video => DomainContentType.Video,
            _ => DomainContentType.NoOne,
        };
    }

    public static ContentTypes ToStorage(this DomainContentType domainType)
    {
        return domainType switch
        {
            DomainContentType.Photo => ContentTypes.Photo,
            DomainContentType.Video => ContentTypes.Video,
            _ => ContentTypes.NoOne,
        };
    }
}