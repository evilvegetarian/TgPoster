using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace TgPoster.OpenApi;

/// <summary>
/// Забирает хост API сразу после builder.Build() и обрывает Program, пока тот не дошёл до миграции БД и app.Run()
/// </summary>
internal sealed class HostCapture
	: IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
	private const string HostingListenerName = "Microsoft.Extensions.Hosting";
	private const string HostBuiltEvent = "HostBuilt";

	private readonly List<IDisposable> subscriptions = [];

	public HostCapture()
	{
		subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
	}

	public IHost? Host { get; private set; }

	public void OnNext(DiagnosticListener value)
	{
		if (value.Name == HostingListenerName)
		{
			subscriptions.Add(value.Subscribe(this));
		}
	}

	public void OnNext(KeyValuePair<string, object?> value)
	{
		if (value.Key == HostBuiltEvent && value.Value is IHost host)
		{
			Host = host;
			throw new HostCapturedException();
		}
	}

	public void OnCompleted()
	{
	}

	public void OnError(Exception error)
	{
	}

	public void Dispose()
	{
		foreach (var subscription in subscriptions)
		{
			subscription.Dispose();
		}
	}
}

internal sealed class HostCapturedException() : Exception("Хост API собран, Program остановлен до запуска");
