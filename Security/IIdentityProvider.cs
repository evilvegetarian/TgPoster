using Security.Models;

namespace Security;

public interface IIdentityProvider
{
    Identity Current { get; set; }
}