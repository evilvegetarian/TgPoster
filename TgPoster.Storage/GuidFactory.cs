using UUIDNext;

namespace TgPoster.Storage;

internal class GuidFactory
{
    public Guid New()
    {
        return Uuid.NewDatabaseFriendly(Database.PostgreSql);
    }
}