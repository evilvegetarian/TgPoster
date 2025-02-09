using Security.Models;

namespace Security.Interfaces;

internal class IdentityProvider : IIdentityProvider
{
    public Identity Current { get; set; }
}