using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using DockGen.Commands.GenerateCommand;
using DockGen.Generator;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

MSBuildLocator.RegisterDefaults();

var rootCommand = new RootCommand("DockGen - Dockerfile Generator for .NET");
rootCommand.AddCommand(new GenerateCommand());
rootCommand.AddCommand(new UpdateCommand());

var builder = new CommandLineBuilder(rootCommand);
builder.UseHost(_ => Host.CreateDefaultBuilder(), hostBuilder =>
    {
        hostBuilder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog();
        });
        hostBuilder.UseSerilog((_, config) =>
        {
            config.WriteTo.Console();
        });
        hostBuilder.UseCommandHandler<GenerateCommand, GenerateCommandHandler>();
        hostBuilder.UseCommandHandler<UpdateCommand, GenerateCommandHandler>();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddGeneratorCore();
        });
    })
    .UseDefaults();

var parser = builder.Build();

return await parser.InvokeAsync(args);
