namespace Auth;

public interface IIdentityProvider
{
    Identity Current { get; set; }
}