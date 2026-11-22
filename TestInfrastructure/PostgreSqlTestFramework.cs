using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace TgPoster.Testing;

public sealed class PostgreSqlTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
	protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
		new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

	private sealed class Executor(
		AssemblyName assemblyName,
		ISourceInformationProvider sourceInformationProvider,
		IMessageSink diagnosticMessageSink)
		: XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
	{
		protected override async void RunTestCases(
			IEnumerable<IXunitTestCase> testCases,
			IMessageSink executionMessageSink,
			ITestFrameworkExecutionOptions executionOptions)
		{
			using var runner = new AssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink,
				executionMessageSink, executionOptions);
			await runner.RunAsync();
		}
	}

	private sealed class AssemblyRunner(
		ITestAssembly testAssembly,
		IEnumerable<IXunitTestCase> testCases,
		IMessageSink diagnosticMessageSink,
		IMessageSink executionMessageSink,
		ITestFrameworkExecutionOptions executionOptions)
		: XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
	{
		protected override async Task BeforeTestAssemblyFinishedAsync()
		{
			await base.BeforeTestAssemblyFinishedAsync();
			await Aggregator.RunAsync(() =>
				PostgreSqlTestDatabase.DisposeContainerAsync(Assembly.Load(TestAssembly.Assembly.Name)));
		}
	}
}
