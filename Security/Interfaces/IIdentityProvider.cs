using Security.Models;

namespace Security.Interfaces;

public interface IIdentityProvider
{
    Identity Current { get; set; }
}