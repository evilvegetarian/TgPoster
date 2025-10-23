using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Security.Models;

namespace Security.Interfaces;

public interface IJwtProvider
{
	string GenerateToken(TokenServiceBuildTokenPayload tokenPayload);
	ClaimsPrincipal GetPrincipalFromToken(string token);
	(Guid RefreshToken, DateTimeOffset RefreshExpireTime) GenerateRefreshToken();
	void AddTokenToCookie(HttpContext httpContext, string accessToken);
}