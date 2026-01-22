using Security.IdentityServices;
using Shouldly;

namespace Security.Tests;

/// <summary>
///     Тесты для класса IdentityProvider
/// </summary>
public sealed class IdentityProviderShould
{
	[Fact]
	public void StartWithAnonymousIdentity()
	{
		var identityProvider = new IdentityProvider();

		identityProvider.Current.ShouldBe(Identity.Anonymous);
		identityProvider.Current.IsAuthenticated.ShouldBeFalse();
		identityProvider.Current.UserId.ShouldBe(Guid.Empty);
	}

	[Fact]
	public void SetAuthenticatedIdentity()
	{
		var identityProvider = new IdentityProvider();
		var userId = Guid.NewGuid();
		var identity = new Identity(userId);

		identityProvider.Set(identity);

		identityProvider.Current.ShouldBe(identity);
		identityProvider.Current.UserId.ShouldBe(userId);
		identityProvider.Current.IsAuthenticated.ShouldBeTrue();
	}

	[Fact]
	public void UpdateIdentityMultipleTimes()
	{
		var identityProvider = new IdentityProvider();
		var userId1 = Guid.NewGuid();
		var userId2 = Guid.NewGuid();
		var identity1 = new Identity(userId1);
		var identity2 = new Identity(userId2);

		identityProvider.Set(identity1);
		identityProvider.Current.UserId.ShouldBe(userId1);

		identityProvider.Set(identity2);
		identityProvider.Current.UserId.ShouldBe(userId2);
	}

	[Fact]
	public void ThrowExceptionWhenSettingNullIdentity()
	{
		var identityProvider = new IdentityProvider();

		Should.Throw<ArgumentNullException>(() => identityProvider.Set(null!));
	}

	[Fact]
	public void KeepPreviousIdentityWhenExceptionThrown()
	{
		var identityProvider = new IdentityProvider();
		var userId = Guid.NewGuid();
		var identity = new Identity(userId);
		identityProvider.Set(identity);

		try
		{
			identityProvider.Set(null!);
		}
		catch (ArgumentNullException)
		{
		}

		identityProvider.Current.ShouldBe(identity);
		identityProvider.Current.UserId.ShouldBe(userId);
	}

	[Fact]
	public void RecognizeAuthenticatedIdentity()
	{
		var userId = Guid.NewGuid();
		var identity = new Identity(userId);

		identity.IsAuthenticated.ShouldBeTrue();
	}

	[Fact]
	public void RecognizeAnonymousIdentity()
	{
		var identity = Identity.Anonymous;

		identity.IsAuthenticated.ShouldBeFalse();
		identity.UserId.ShouldBe(Guid.Empty);
	}

	[Fact]
	public void RecognizeIdentityWithEmptyGuidAsAnonymous()
	{
		var identity = new Identity(Guid.Empty);

		identity.IsAuthenticated.ShouldBeFalse();
	}
}