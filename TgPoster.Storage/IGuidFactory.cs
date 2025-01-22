using UUIDNext;

namespace TgPoster.Storage;

internal interface IGuidFactory
{
    Guid New();
}

internal class GuidFactory : IGuidFactory
{
    public Guid New() => Uuid.NewDatabaseFriendly(Database.PostgreSql);
}