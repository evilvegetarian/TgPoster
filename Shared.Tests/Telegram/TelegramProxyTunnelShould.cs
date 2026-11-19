using System.Net;
using System.Text;
using Shared.Telegram;
using Shouldly;

namespace Shared.Tests.Telegram;

public sealed class TelegramProxyTunnelShould
{
	[Fact]
	public async Task ReadStatusWithoutConsumingTlsBytesAfterHeaders()
	{
		var header = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection established\r\nProxy-Agent: test\r\n\r\n");
		using var stream = new MemoryStream([..header, 0x16, 0x03, 0x03]);

		var status = await TelegramBotManager.ReadTunnelStatusAsync(stream, CancellationToken.None);

		status.ShouldBe(HttpStatusCode.OK);
		stream.Position.ShouldBe(header.Length);
		stream.ReadByte().ShouldBe(0x16);
	}

	[Theory]
	[InlineData("")]
	[InlineData("HTTP/1.1 200 OK\r\n")]
	[InlineData("malformed\r\n\r\n")]
	public async Task RejectIncompleteOrMalformedProxyReply(string response)
	{
		using var stream = new MemoryStream(Encoding.ASCII.GetBytes(response));

		await Should.ThrowAsync<HttpRequestException>(() => TelegramBotManager.ReadTunnelStatusAsync(stream, CancellationToken.None).AsTask());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SendConnectWithPreemptiveBasicAuthentication_WhenCredentialsExist(bool authenticated)
	{
		var uri = new Uri("http://proxy.example.test:8080");
		var proxy = new WebProxy(uri);
		if (authenticated) proxy.Credentials = new NetworkCredential("user", "password");
		using var manager = new TelegramBotManager(proxy);
		using var stream = new DuplexStream("HTTP/1.1 200 OK\r\n\r\n");

		await manager.EstablishTunnelAsync(stream, "api.telegram.org", 443, uri, CancellationToken.None);

		var request = Encoding.ASCII.GetString(stream.Written.ToArray());
		request.ShouldStartWith("CONNECT api.telegram.org:443 HTTP/1.1\r\nHost: api.telegram.org:443\r\n");
		request.ShouldEndWith("\r\n\r\n");
		if (authenticated) request.ShouldContain("Proxy-Authorization: Basic " + Convert.ToBase64String("user:password"u8));
		else request.ShouldNotContain("Proxy-Authorization");
	}

	[Fact]
	public async Task RejectTunnel_WhenProxyReturnsForbidden()
	{
		var uri = new Uri("http://proxy.example.test:8080");
		using var manager = new TelegramBotManager(new WebProxy(uri));
		using var stream = new DuplexStream("HTTP/1.1 403 Forbidden\r\n\r\n");

		var exception = await Should.ThrowAsync<HttpRequestException>(() =>
			manager.EstablishTunnelAsync(stream, "api.telegram.org", 443, uri, CancellationToken.None).AsTask());

		exception.Message.ShouldContain("HTTP 403");
	}

	private sealed class DuplexStream(string response) : Stream
	{
		private readonly MemoryStream readable = new(Encoding.ASCII.GetBytes(response));
		public MemoryStream Written { get; } = new();
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => readable.Length;
		public override long Position { get => readable.Position; set => throw new NotSupportedException(); }
		public override int Read(byte[] buffer, int offset, int count) => readable.Read(buffer, offset, count);
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => readable.ReadAsync(buffer, ct);
		public override void Write(byte[] buffer, int offset, int count) => Written.Write(buffer, offset, count);
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => Written.WriteAsync(buffer, ct);
		public override void Flush() { }
		public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		protected override void Dispose(bool disposing)
		{
			if (disposing) { readable.Dispose(); Written.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
