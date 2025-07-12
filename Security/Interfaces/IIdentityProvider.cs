using Security.Models;

namespace Security.Interfaces;

public interface IIdentityProvider
{
    Identity Current { get; }

    void Set(Identity identity);
}