namespace Security.IdentityServices;

public interface IIdentityProvider
{
	Identity Current { get; }

	void Set(Identity identity);
}