using Shared.Enums;
using TgPoster.API.Domain.UseCases.Proxies.CreateProxy;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateProxyStorage(PosterContext context, GuidFactory guidFactory) : ICreateProxyStorage
{
	public async Task<Guid> CreateAsync(
		Guid userId,
		string name,
		ProxyType type,
		string host,
		int port,
		string? username,
		string? password,
		string? secret,
		CancellationToken ct)
	{
		var proxy = new Proxy
		{
			Id = guidFactory.New(),
			UserId = userId,
			Name = name,
			Type = (Data.Enum.ProxyType)type,
			Host = host,
			Port = port,
			Username = username,
			Password = password,
			Secret = secret
		};

		await context.Proxies.AddAsync(proxy, ct);
		await context.SaveChangesAsync(ct);
		return proxy.Id;
	}
}
