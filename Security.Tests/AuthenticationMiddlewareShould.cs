using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Security.Authentication;
using Security.IdentityServices;
using Security.Middleware;
using Shouldly;

namespace Security.Tests;

/// <summary>
///     Тесты для класса AuthenticationMiddleware
/// </summary>
public sealed class AuthenticationMiddlewareShould
{
	[Fact]
	public async Task SetIdentityForAuthenticatedUser()
	{
		var userId = Guid.NewGuid();
		var claims = new List<Claim>
		{
			new(JwtClaimTypes.UserId, userId.ToString())
		};
		var identity = new ClaimsIdentity(claims, "TestAuthType");
		var claimsPrincipal = new ClaimsPrincipal(identity);

		var context = new DefaultHttpContext
		{
			User = claimsPrincipal
		};

		var identityProvider = new IdentityProvider();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		nextCalled.ShouldBeTrue();
		identityProvider.Current.UserId.ShouldBe(userId);
		identityProvider.Current.IsAuthenticated.ShouldBeTrue();
	}

	[Fact]
	public async Task KeepAnonymousIdentityForUnauthenticatedUser()
	{
		var context = new DefaultHttpContext
		{
			User = new ClaimsPrincipal()
		};

		var identityProvider = new IdentityProvider();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		nextCalled.ShouldBeTrue();
		identityProvider.Current.ShouldBe(Identity.Anonymous);
		identityProvider.Current.IsAuthenticated.ShouldBeFalse();
	}

	[Fact]
	public async Task KeepAnonymousIdentityWhenUserIdClaimMissing()
	{
		var claims = new List<Claim>
		{
			new("SomeOtherClaim", "value")
		};
		var identity = new ClaimsIdentity(claims, "TestAuthType");
		var claimsPrincipal = new ClaimsPrincipal(identity);

		var context = new DefaultHttpContext
		{
			User = claimsPrincipal
		};

		var identityProvider = new IdentityProvider();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		nextCalled.ShouldBeTrue();
		identityProvider.Current.ShouldBe(Identity.Anonymous);
	}

	[Fact]
	public async Task KeepAnonymousIdentityWhenUserIdIsInvalidGuid()
	{
		var claims = new List<Claim>
		{
			new(JwtClaimTypes.UserId, "not-a-valid-guid")
		};
		var identity = new ClaimsIdentity(claims, "TestAuthType");
		var claimsPrincipal = new ClaimsPrincipal(identity);

		var context = new DefaultHttpContext
		{
			User = claimsPrincipal
		};

		var identityProvider = new IdentityProvider();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		nextCalled.ShouldBeTrue();
		identityProvider.Current.ShouldBe(Identity.Anonymous);
	}

	[Fact]
	public async Task CallNextDelegateInAllCases()
	{
		var context = new DefaultHttpContext();
		var identityProvider = new IdentityProvider();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		nextCalled.ShouldBeTrue();
	}

	[Fact]
	public async Task PropagateExceptionFromNextDelegate()
	{
		var context = new DefaultHttpContext();
		var identityProvider = new IdentityProvider();
		var expectedException = new InvalidOperationException("Test exception");
		RequestDelegate next = _ => throw expectedException;

		var middleware = new AuthenticationMiddleware(next);

		var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
			await middleware.InvokeAsync(context, identityProvider));

		exception.ShouldBe(expectedException);
	}

	[Fact]
	public async Task HandleMultipleUserIdClaims()
	{
		var userId = Guid.NewGuid();
		var claims = new List<Claim>
		{
			new(JwtClaimTypes.UserId, userId.ToString()),
			new(JwtClaimTypes.UserId, Guid.NewGuid().ToString())
		};
		var identity = new ClaimsIdentity(claims, "TestAuthType");
		var claimsPrincipal = new ClaimsPrincipal(identity);

		var context = new DefaultHttpContext
		{
			User = claimsPrincipal
		};

		var identityProvider = new IdentityProvider();
		RequestDelegate next = _ => Task.CompletedTask;
		var middleware = new AuthenticationMiddleware(next);

		await middleware.InvokeAsync(context, identityProvider);

		identityProvider.Current.UserId.ShouldBe(userId);
	}
}