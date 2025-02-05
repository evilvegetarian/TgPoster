namespace Security.Models;

public class Identity(Guid userId)
{
    public Guid UserId { get; } = userId;
}