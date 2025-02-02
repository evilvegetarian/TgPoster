namespace Auth;

public class Identity
{
    public Identity(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}