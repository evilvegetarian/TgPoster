using Shared.Telegram;
using Xunit;

namespace Shared.Tests.Telegram;

public sealed class TelegramBotManagerShould : IDisposable
{
	private readonly TelegramBotManager manager = new();

	[Fact]
	public void ReturnSameClientForSameToken()
	{
		const string token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";

		var client1 = manager.GetClient(token);
		var client2 = manager.GetClient(token);

		Assert.Same(client1, client2);
	}

	[Fact]
	public void ReturnDifferentClientsForDifferentTokens()
	{
		const string token1 = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
		const string token2 = "654321:XYZ-ABC9876ghIkl-abc12W3v4u567ew89";

		var client1 = manager.GetClient(token1);
		var client2 = manager.GetClient(token2);

		Assert.NotSame(client1, client2);
	}

	[Fact]
	public void RemoveClientFromCache()
	{
		const string token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";

		var client1 = manager.GetClient(token);
		var removed = manager.RemoveClient(token);
		var client2 = manager.GetClient(token);

		Assert.True(removed);
		Assert.NotSame(client1, client2);
	}

	[Fact]
	public void ReturnFalseWhenRemovingNonExistentClient()
	{
		const string token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";

		var removed = manager.RemoveClient(token);

		Assert.False(removed);
	}

	public void Dispose()
	{
		manager.Dispose();
	}
}
