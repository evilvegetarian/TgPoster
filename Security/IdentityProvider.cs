using Security.Models;

namespace Security;

internal class IdentityProvider : IIdentityProvider
{
    public Identity Current { get; set; }
}