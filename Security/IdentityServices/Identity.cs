namespace Security.IdentityServices;

public record Identity(Guid UserId)
{
	public bool IsAuthenticated => UserId != Guid.Empty;
	public static Identity Anonymous => new(Guid.Empty);
}