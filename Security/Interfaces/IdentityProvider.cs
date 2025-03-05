using Security.Models;

namespace Security.Interfaces;

internal class IdentityProvider : IIdentityProvider
{
    public required Identity Current { get; set; }
}