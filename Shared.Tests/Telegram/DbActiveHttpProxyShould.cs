using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Configuration;
using TgPoster.Telegram.Internal;
using TgPoster.Telegram.Models;

namespace Shared.Tests.Telegram;

public sealed class DbActiveHttpProxyShould
{
	private static readonly Uri Destination = new("https://t.me/channel");
	private readonly Mock<ITelegramHttpProxyRepository> repository = new();

	[Fact]
	public void LoadProxyAndCredentialsBeforeReturningFirstResult_AndReuseCachedSnapshot()
	{
		repository.Setup(x => x.GetActiveHttpProxyAsync(CancellationToken.None))
			.ReturnsAsync(new ProxyDto(ProxyType.Http, "proxy.example.test", 8080, "user", "password", null));
		using var services = Services();
		var sut = Create(services);

		var proxy = sut.GetProxy(Destination);
		var credentials = sut.Credentials?.GetCredential(proxy!, "Basic");
		sut.Credentials = new NetworkCredential("ignored", "ignored");

		proxy.ShouldBe(new Uri("http://proxy.example.test:8080"));
		credentials.ShouldNotBeNull();
		credentials.UserName.ShouldBe("user");
		credentials.Password.ShouldBe("password");
		sut.IsBypassed(Destination).ShouldBeFalse();
		sut.Credentials!.GetCredential(proxy!, "Basic")!.UserName.ShouldBe("user");
		repository.Verify(x => x.GetActiveHttpProxyAsync(CancellationToken.None), Times.Once);
	}

	[Fact]
	public void BypassProxy_WhenNoHttpProxyIsConfigured()
	{
		using var services = Services();
		var sut = Create(services);

		sut.GetProxy(Destination).ShouldBeNull();
		sut.Credentials.ShouldBeNull();
		sut.IsBypassed(Destination).ShouldBeTrue();
		repository.Verify(x => x.GetActiveHttpProxyAsync(CancellationToken.None), Times.Once);
	}

	[Fact]
	public void OmitCredentials_WhenUsernameIsEmpty()
	{
		repository.Setup(x => x.GetActiveHttpProxyAsync(CancellationToken.None))
			.ReturnsAsync(new ProxyDto(ProxyType.Http, "proxy.example.test", 8080, " ", "password", null));
		using var services = Services();

		Create(services).Credentials.ShouldBeNull();
	}

	[Fact]
	public void FallBackToDirectConnection_WhenInitialLoadingFails()
	{
		repository.Setup(x => x.GetActiveHttpProxyAsync(CancellationToken.None)).ThrowsAsync(new IOException("database unavailable"));
		using var services = Services();

		Create(services).GetProxy(Destination).ShouldBeNull();
	}

	[Fact]
	public void LoadInitialSnapshotOnlyOnce_WhenSeveralThreadsRequestProxy()
	{
		using var services = Services();
		var sut = Create(services);

		Parallel.For(0, 20, _ => sut.GetProxy(Destination));

		repository.Verify(x => x.GetActiveHttpProxyAsync(CancellationToken.None), Times.Once);
	}

	private ServiceProvider Services() => new ServiceCollection().AddSingleton(repository.Object).BuildServiceProvider();
	private static DbActiveHttpProxy Create(IServiceProvider services) => new(
		services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new TelegramPublicLookupOptions()),
		NullLogger<DbActiveHttpProxy>.Instance);
}
