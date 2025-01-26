using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Auth;

public interface IJwtProvider
{
    string GenerateToken(TokenServiceBuildTokenPayload tokenPayload);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    (Guid RefreshToken, DateTimeOffset RefreshExpireTime) GenerateRefreshToken();
    void AddTokenToCookie(HttpContext httpContext, string accessToken);
}