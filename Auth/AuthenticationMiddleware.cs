using Auth.Models;
using Microsoft.AspNetCore.Http;

namespace Auth;

public class AuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IHttpContextAccessor httpContextAccessor,
        IIdentityProvider identityProvider
    )
    {
        var stringId = httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimTypes.UserId)?.Value;
        var canParse = Guid.TryParse(stringId, out Guid userId);
        identityProvider.Current = new Identity(canParse ? userId : Guid.Empty);

        await next(context);
    }
}