using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using DockGen.Commands.GenerateCommand;
using DockGen.Generator;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

MSBuildLocator.RegisterDefaults();

var rootCommand = new RootCommand("DockGen - Dockerfile Generator for .NET");
rootCommand.AddCommand(new GenerateCommand());

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
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<IFileProvider>(sp =>
            {
                var parseResult = sp.GetRequiredService<ParseResult>();
                var directoryPath = parseResult.CommandResult.GetValueForOption(GenerateCommand.DirectoryOption);
                var solutionPath = parseResult.CommandResult.GetValueForOption(GenerateCommand.SolutionOption);
                var projectPath = parseResult.CommandResult.GetValueForOption(GenerateCommand.ProjectOption);

                if (!string.IsNullOrEmpty(directoryPath))
                {
                    var path = Path.GetFullPath(directoryPath);
                    return new PhysicalFileProvider(path);
                }

                if (!string.IsNullOrEmpty(solutionPath))
                {
                    var path = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
                    return new PhysicalFileProvider(path);
                }

                if (!string.IsNullOrEmpty(projectPath))
                {
                    var path = Path.GetDirectoryName(Path.GetFullPath(projectPath));
                    return new PhysicalFileProvider(path);
                }

                var env = sp.GetRequiredService<IHostEnvironment>();
                return env.ContentRootFileProvider;
            });

            services.AddScoped<IDockGenAnalyser, PlainAnalyser>();
            services.AddScoped<IProjectFileLocator, ProjectFileLocator>();
            services.AddGeneratorCore();
        });
    })
    .UseDefaults();

var parser = builder.Build();

return await parser.InvokeAsync(args);
