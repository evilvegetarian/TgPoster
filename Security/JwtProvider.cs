using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Security.Interfaces;
using Security.Models;

namespace Security;

internal class JwtProvider(JwtOptions options) : IJwtProvider
{
    public string GenerateToken(TokenServiceBuildTokenPayload tokenPayload)
    {
        Claim[] claims =
        [
            new(JwtClaimTypes.UserId, tokenPayload.UserId.ToString())
        ];

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var expireTime = DateTime.UtcNow.AddHours(options.AccessExpiresHours);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: expireTime,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid RefreshToken, DateTimeOffset RefreshExpireTime) GenerateRefreshToken()
    {
        return (Guid.NewGuid(),
            DateTime.UtcNow.AddHours(options.RefreshTokenExpiresHours));
    }

    public void AddTokenToCookie(HttpContext httpContext, string accessToken)
    {
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };
        httpContext.Response.Cookies.Append(options.NameCookie, accessToken, cookieOptions);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (!(securityToken is JwtSecurityToken jwtSecurityToken)
            || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}