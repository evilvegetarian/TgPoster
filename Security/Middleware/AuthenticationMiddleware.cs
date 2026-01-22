using Microsoft.AspNetCore.Http;
using Security.Authentication;
using Security.IdentityServices;

namespace Security.Middleware;

public class AuthenticationMiddleware(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context, IIdentityProvider identityProvider)
	{
		if (context.User.Identity?.IsAuthenticated == true)
		{
			var userIdClaim = context.User.FindFirst(JwtClaimTypes.UserId);

			if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
			{
				(identityProvider as IdentityProvider)?.Set(new Identity(userId));
			}
		}

		await next(context);
	}
}