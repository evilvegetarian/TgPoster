using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using TgPoster.OpenApi;

var outputPath = Path.GetFullPath(args.Length > 0 ? args[0] : "openapi.json");
var apiAssembly = typeof(TgPoster.API.Program).Assembly;

// Окружение не Development, поэтому user-secrets не подгружаются. Заглушки нужны только чтобы собрать
// DI-контейнер: к ним никто не подключается, а командная строка перекрывает и переменные окружения
string[] apiArgs =
[
	"--environment=OpenApi",
	$"--applicationName={apiAssembly.GetName().Name}",
	$"--contentRoot={AppContext.BaseDirectory}",
	"--DataBase:ConnectionString=Host=127.0.0.1;Port=1;Database=openapi",
	"--JwtOptions:SecretKey=openapi",
	"--TelegramOptions:SecretKey=openapi",
	"--OpenRouterOptions:SecretKey=openapi",
	"--S3Options:ServiceUrl=http://127.0.0.1:1",
	"--TracingConfiguration:Url=http://127.0.0.1:1"
];

using var capture = new HostCapture();
try
{
	apiAssembly.EntryPoint!.Invoke(null, [apiArgs]);
}
catch (TargetInvocationException e) when (e.InnerException is HostCapturedException)
{
}

if (capture.Host is null)
{
	throw new InvalidOperationException("API завершился, не собрав хост");
}

var document = capture.Host.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
await using (var writer = new StreamWriter(outputPath))
{
	document.SerializeAsV3(new OpenApiJsonWriter(writer));
}

Console.WriteLine($"OpenAPI: {outputPath}");
