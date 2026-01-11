using System.CommandLine;
using System.Reflection;
using System.Text;
using DockGen;
using DockGen.Commands.GenerateCommand;
using DockGen.Commands.UpdateCommand;
using DockGen.Generator;
using DockGen.Generator.Constants;
using DockGen.Generator.Evaluators;
using DockGen.Generator.Locators;
using DockGen.Generator.Properties;
using DockGen.Logging;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

Console.OutputEncoding = Encoding.UTF8;

MSBuildLocator.RegisterDefaults();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    cts.Cancel();
    eventArgs.Cancel = true;
};

var settings = new HostApplicationBuilderSettings
{
    Configuration = new ConfigurationManager()
};
settings.Configuration.AddEnvironmentVariables();

var builder = Host.CreateEmptyApplicationBuilder(settings);
builder.Logging.ClearProviders();

var rootCommand = new RootCommand("DockGen - Dockerfile Generator for .NET");
rootCommand.Add(LogLevelOptions.Detailed);

var commandHostBuilder = builder.Services.AddRootCommand(rootCommand);
commandHostBuilder.AddCommand<GenerateCommand, GenerateCommandHandler>();
commandHostBuilder.AddCommand<UpdateCommand, UpdateCommandHandler>();

commandHostBuilder.Build(args);

var parseResult = commandHostBuilder.RootCommand.Parse(args);

builder.Services.AddFileProvider(parseResult);

var detailed = parseResult.GetValue(LogLevelOptions.Detailed);
var minimumLevel = detailed ? LogEventLevel.Verbose : LogEventLevel.Information;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(minimumLevel)
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger);
builder.Services.AddScoped<IAnalyzer, Analyzer>();
builder.Services.AddKeyedScoped<IProjectEvaluator, SimpleProjectEvaluator>(DockGenConstants.SimpleAnalyzerName);
builder.Services.AddKeyedScoped<IProjectEvaluator, BuildalyzerProjectEvaluator>(DockGenConstants.DesignBuildTimeAnalyzerName);
builder.Services.AddKeyedScoped<IProjectEvaluator, FastProjectEvaluator>(DockGenConstants.FastAnalyzerName);
builder.Services.AddScoped<IProjectFileLocator, ProjectFileLocator>();
builder.Services.AddScoped<IRelevantFileLocator, RelevantFileLocator>();
builder.Services.AddSingleton<DockerfileGenerator>();
builder.Services.AddTransient<IExtractor, Extractor>();
builder.Services.AddExtractorsFromAssembly(Assembly.GetExecutingAssembly());

using var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var executionService = app.Services.GetRequiredService<ExecutionService>();

var invokeConfig = new InvocationConfiguration()
{
    EnableDefaultExceptionHandler = true
};

var exitCode = await executionService.ExecuteAsync(invokeConfig, cts.Token);

await app.StopAsync().ConfigureAwait(false);

return exitCode;

