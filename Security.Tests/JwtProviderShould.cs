using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Security.Authentication;
using Shouldly;

namespace Security.Tests;

/// <summary>
///     Тесты для класса JwtProvider
/// </summary>
public sealed class JwtProviderShould
{
	private readonly JwtOptions jwtOptions;
	private readonly IJwtProvider jwtProvider;

	public JwtProviderShould()
	{
		jwtOptions = new JwtOptions
		{
			SecretKey = "ThisIsAVerySecureSecretKeyForTesting1234567890",
			AccessExpiresHours = 1,
			RefreshTokenExpiresHours = 24,
			NameCookie = "auth_token"
		};

		jwtProvider = new JwtProvider(jwtOptions);
	}

	[Fact]
	public void GenerateTokenForUser()
	{
		var userId = Guid.NewGuid();
		var payload = new TokenServiceBuildTokenPayload(userId);

		var token = jwtProvider.GenerateToken(payload);

		token.ShouldNotBeNullOrWhiteSpace();
		var handler = new JwtSecurityTokenHandler();
		var jwtToken = handler.ReadJwtToken(token);
		jwtToken.Claims.ShouldContain(c => c.Type == JwtClaimTypes.UserId && c.Value == userId.ToString());
	}

	[Fact]
	public void GenerateTokenWithCorrectExpiration()
	{
		var userId = Guid.NewGuid();
		var payload = new TokenServiceBuildTokenPayload(userId);
		var beforeGeneration = DateTime.UtcNow;

		var token = jwtProvider.GenerateToken(payload);

		var handler = new JwtSecurityTokenHandler();
		var jwtToken = handler.ReadJwtToken(token);
		var expectedExpiration = beforeGeneration.AddHours(jwtOptions.AccessExpiresHours);
		jwtToken.ValidTo.ShouldBeInRange(expectedExpiration.AddSeconds(-5), expectedExpiration.AddSeconds(5));
	}

	[Fact]
	public void GenerateDifferentTokensForDifferentUsers()
	{
		var userId1 = Guid.NewGuid();
		var userId2 = Guid.NewGuid();
		var payload1 = new TokenServiceBuildTokenPayload(userId1);
		var payload2 = new TokenServiceBuildTokenPayload(userId2);

		var token1 = jwtProvider.GenerateToken(payload1);
		var token2 = jwtProvider.GenerateToken(payload2);

		token1.ShouldNotBe(token2);
	}

	[Fact]
	public void GenerateRefreshToken()
	{
		var beforeGeneration = DateTime.UtcNow;

		var (refreshToken, expireTime) = jwtProvider.GenerateRefreshToken();

		refreshToken.ShouldNotBe(Guid.Empty);
		var expectedExpiration = beforeGeneration.AddHours(jwtOptions.RefreshTokenExpiresHours);
		expireTime.ShouldBeInRange(expectedExpiration.AddSeconds(-5), expectedExpiration.AddSeconds(5));
	}

	[Fact]
	public void GenerateUniqueRefreshTokens()
	{
		var (refreshToken1, _) = jwtProvider.GenerateRefreshToken();
		var (refreshToken2, _) = jwtProvider.GenerateRefreshToken();

		refreshToken1.ShouldNotBe(refreshToken2);
	}

	[Fact]
	public void AddTokenToCookie()
	{
		var httpContext = new DefaultHttpContext();
		const string token = "test_token_value";

		jwtProvider.AddTokenToCookie(httpContext, token);

		httpContext.Response.Headers.ShouldContain(h => h.Key == "Set-Cookie");
		var cookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
		cookieHeader.ShouldContain(jwtOptions.NameCookie);
		cookieHeader.ShouldContain(token);
		cookieHeader.ShouldContain("httponly");
		cookieHeader.ShouldContain("secure");
		cookieHeader.ShouldContain("samesite=strict");
	}

	[Fact]
	public void GetPrincipalFromValidToken()
	{
		var userId = Guid.NewGuid();
		var payload = new TokenServiceBuildTokenPayload(userId);
		var token = jwtProvider.GenerateToken(payload);

		var principal = jwtProvider.GetPrincipalFromToken(token);

		principal.ShouldNotBeNull();
		var userIdClaim = principal.FindFirst(JwtClaimTypes.UserId);
		userIdClaim.ShouldNotBeNull();
		userIdClaim.Value.ShouldBe(userId.ToString());
	}

	[Fact]
	public void ThrowExceptionForInvalidToken()
	{
		const string invalidToken = "invalid.token.here";

		Should.Throw<Exception>(() => jwtProvider.GetPrincipalFromToken(invalidToken));
	}

	[Fact]
	public void ThrowExceptionForTokenWithWrongAlgorithm()
	{
		var claims = new[] { new Claim(JwtClaimTypes.UserId, Guid.NewGuid().ToString()) };
		var wrongKey =
			new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(
					"WrongSecretKeyForTestingPurposesWithEnoughLength123456789012345678901234567890"));
		var wrongCredentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha512);
		var wrongToken = new JwtSecurityToken(
			claims: claims,
			expires: DateTime.UtcNow.AddHours(1),
			signingCredentials: wrongCredentials);
		var tokenString = new JwtSecurityTokenHandler().WriteToken(wrongToken);

		Should.Throw<SecurityTokenException>(() => jwtProvider.GetPrincipalFromToken(tokenString));
	}

	[Fact]
	public void AcceptExpiredTokenWhenValidationDisabled()
	{
		var expiredOptions = new JwtOptions
		{
			SecretKey = jwtOptions.SecretKey,
			AccessExpiresHours = -1,
			RefreshTokenExpiresHours = 24,
			NameCookie = "auth_token"
		};
		var expiredProvider = new JwtProvider(expiredOptions);
		var userId = Guid.NewGuid();
		var payload = new TokenServiceBuildTokenPayload(userId);
		var token = expiredProvider.GenerateToken(payload);

		var principal = jwtProvider.GetPrincipalFromToken(token);

		principal.ShouldNotBeNull();
	}
}