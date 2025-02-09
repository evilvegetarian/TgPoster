using UUIDNext;

namespace TgPoster.Storage;

internal class GuidFactory
{
    public Guid New() => Uuid.NewDatabaseFriendly(Database.PostgreSql);
}