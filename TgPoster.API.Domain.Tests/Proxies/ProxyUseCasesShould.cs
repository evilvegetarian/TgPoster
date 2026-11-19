using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Proxies;
using TgPoster.API.Domain.UseCases.Proxies.CreateProxy;
using TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;
using TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.Tests.Proxies;

public sealed class ProxyUseCasesShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Guid proxyId = Guid.NewGuid();
	private readonly Mock<ITelegramAuthService> auth = new();
	private readonly IIdentityProvider identity;

	public ProxyUseCasesShould() => identity = Mock.Of<IIdentityProvider>(x => x.Current == new Identity(userId));

	[Theory]
	[InlineData("", 80, ProxyType.Http, null)]
	[InlineData("  ", 80, ProxyType.Http, null)]
	[InlineData("host", 0, ProxyType.Http, null)]
	[InlineData("host", -1, ProxyType.Http, null)]
	[InlineData("host", 65536, ProxyType.Http, null)]
	[InlineData("host", 80, ProxyType.MTProxy, null)]
	[InlineData("host", 80, ProxyType.MTProxy, " ")]
	public void RejectInvalidProxy(string host, int port, ProxyType type, string? secret)
	{
		Should.Throw<InvalidProxyException>(() => ProxyValidation.Validate(type, host, port, secret));
	}

	[Theory]
	[InlineData(1, ProxyType.Http, null)]
	[InlineData(65535, ProxyType.Socks5, null)]
	[InlineData(443, ProxyType.MTProxy, "secret")]
	public void AcceptValidProxy(int port, ProxyType type, string? secret)
	{
		Should.NotThrow(() => ProxyValidation.Validate(type, "host", port, secret));
	}

	[Theory]
	[InlineData(ProxyType.Http, null)]
	[InlineData(ProxyType.Socks5, null)]
	[InlineData(ProxyType.MTProxy, "secret")]
	public async Task CreateForCurrentUser_WithNormalizedOptionalFields(ProxyType type, string? expectedSecret)
	{
		var storage = new Mock<ICreateProxyStorage>();
		storage.Setup(x => x.CreateAsync(userId, "proxy", type, "host", 443, null, null, expectedSecret,
			It.IsAny<CancellationToken>())).ReturnsAsync(proxyId);
		var sut = new CreateProxyUseCase(storage.Object, identity);

		var response = await sut.Handle(new("proxy", type, "host", 443, " ", "", "secret"), CancellationToken.None);

		response.Id.ShouldBe(proxyId);
		storage.VerifyAll();
	}

	[Fact]
	public async Task NotCreate_WhenValidationFails()
	{
		var storage = new Mock<ICreateProxyStorage>(MockBehavior.Strict);
		var sut = new CreateProxyUseCase(storage.Object, identity);

		await Should.ThrowAsync<InvalidProxyException>(() =>
			sut.Handle(new("proxy", ProxyType.Http, "host", 0, null, null, null), CancellationToken.None));

		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task NotUpdateOrReconnect_WhenProxyIsNotOwned()
	{
		var storage = new Mock<IUpdateProxyStorage>();
		var sut = new UpdateProxyUseCase(storage.Object, identity, auth.Object);

		await Should.ThrowAsync<ProxyNotFoundException>(() => sut.Handle(Update(), CancellationToken.None));

		storage.Verify(x => x.ExistsAsync(userId, proxyId, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ProxyType>(),
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
			It.IsAny<CancellationToken>()), Times.Never);
		auth.VerifyNoOtherCalls();
	}

	[Theory]
	[InlineData(ProxyType.Http, null)]
	[InlineData(ProxyType.MTProxy, "secret")]
	public async Task UpdateAndReconnectAffectedSessions(ProxyType type, string? expectedSecret)
	{
		var sessions = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
		var storage = new Mock<IUpdateProxyStorage>();
		storage.Setup(x => x.ExistsAsync(userId, proxyId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
		storage.Setup(x => x.UpdateAsync(proxyId, "proxy", type, "host", 443, "user", null, expectedSecret,
			It.IsAny<CancellationToken>())).ReturnsAsync(sessions);
		var sut = new UpdateProxyUseCase(storage.Object, identity, auth.Object);

		await sut.Handle(Update() with { Type = type }, CancellationToken.None);

		storage.VerifyAll();
		foreach (var session in sessions)
			auth.Verify(x => x.RemoveClientAsync(session), Times.Once);
	}

	[Fact]
	public async Task NotDelete_WhenProxyIsNotOwned()
	{
		var storage = new Mock<IDeleteProxyStorage>();
		var sut = new DeleteProxyUseCase(storage.Object, identity, auth.Object);

		await Should.ThrowAsync<ProxyNotFoundException>(() => sut.Handle(new(proxyId), CancellationToken.None));

		storage.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		auth.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task DeleteAndReconnectAffectedSessions()
	{
		var session = Guid.NewGuid();
		var storage = new Mock<IDeleteProxyStorage>();
		storage.Setup(x => x.ExistsAsync(userId, proxyId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
		storage.Setup(x => x.DeleteAsync(proxyId, It.IsAny<CancellationToken>())).ReturnsAsync([session]);
		var sut = new DeleteProxyUseCase(storage.Object, identity, auth.Object);

		await sut.Handle(new(proxyId), CancellationToken.None);

		auth.Verify(x => x.RemoveClientAsync(session), Times.Once);
	}

	[Fact]
	public async Task ListOnlyCurrentUserProxies()
	{
		var storage = new Mock<IListProxiesStorage>();
		var items = new List<ProxyResponse> { new(proxyId, "proxy", ProxyType.Http, "host", 80, null, null, null, 2, null) };
		storage.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

		var response = await new ListProxiesUseCase(storage.Object, identity).Handle(new(), CancellationToken.None);

		response.Items.ShouldBe(items);
		storage.VerifyAll();
	}

	private UpdateProxyCommand Update() => new(proxyId, "proxy", ProxyType.Http, "host", 443, "user", " ", "secret");
}
