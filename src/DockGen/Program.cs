using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using DockGen.Commands.GenerateCommand;
using DockGen.Generator;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

MSBuildLocator.RegisterDefaults();

var rootCommand = new RootCommand("DockGen - Dockerfile Generator for .NET");
rootCommand.AddCommand(new GenerateCommand());

var builder = new CommandLineBuilder(rootCommand);
builder.UseHost(_ => Host.CreateDefaultBuilder(), hostBuilder =>
{
    hostBuilder.UseCommandHandler<GenerateCommand, GenerateCommandHandler>();
    hostBuilder.ConfigureServices(services =>
    {
        services.AddSingleton<DockerfileGenerator>();
    });
})
.UseDefaults();

var parser = builder.Build();

return await parser.InvokeAsync(args);