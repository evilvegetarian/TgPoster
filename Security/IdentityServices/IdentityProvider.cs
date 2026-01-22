namespace Security.IdentityServices;

internal class IdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity ?? throw new ArgumentNullException(nameof(identity));
	}
}