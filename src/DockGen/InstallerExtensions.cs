using System.CommandLine;
using System.Reflection;
using DockGen.Commands.GenerateCommand;
using DockGen.Commands.UpdateCommand;
using DockGen.Generator.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DockGen;

public static class InstallerExtensions
{
    public static CommandHostBuilder AddRootCommand(this IServiceCollection services, RootCommand rootCommand)
    {
        services.AddTransient<ExecutionService>();

        return new CommandHostBuilder(services, rootCommand);
    }

    public static void AddExtractorsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var extractors = assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IExtractRequestHandler<,>)));
        foreach (var extractor in extractors)
        {
            var interfaces = extractor.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IExtractRequestHandler<,>));
            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, extractor);
            }
        }
    }

    public static void AddFileProvider(this IServiceCollection services, string[] args)
    {
        var fileProvider = ConfigureFileProvider(args);
        services.AddSingleton(fileProvider);
    }

    private static IFileProvider ConfigureFileProvider(string[] args)
    {
        // in beta4 of System.CommandLine it was simpler to parse those directories without creating a fake root command
        // there may be a better way to do this in the future, I'll revisit later
        var tempRootCommand = new RootCommand("Temp root command for parsing directory options");
        tempRootCommand.Add(new GenerateCommand());
        tempRootCommand.Add(new UpdateCommand());

        var parseResult = tempRootCommand.Parse(args);

        var directoryPath = parseResult.CommandResult.GetValue(GenerateCommand.DirectoryOption) ?? parseResult.CommandResult.GetValue(UpdateCommand.DirectoryOption);
        var solutionPath = parseResult.CommandResult.GetValue(GenerateCommand.SolutionOption) ?? parseResult.CommandResult.GetValue(UpdateCommand.SolutionOption);
        var projectPath = parseResult.CommandResult.GetValue(GenerateCommand.ProjectOption) ?? parseResult.CommandResult.GetValue(UpdateCommand.ProjectOption);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            var path = Path.GetFullPath(directoryPath);
            Directory.SetCurrentDirectory(path);
            return new PhysicalFileProvider(path);
        }

        if (!string.IsNullOrEmpty(solutionPath))
        {
            var path = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;

            Directory.SetCurrentDirectory(path);
            return new PhysicalFileProvider(path);
        }

        if (!string.IsNullOrEmpty(projectPath))
        {
            var path = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
            return new PhysicalFileProvider(path);
        }

        var workingDirectory = Environment.CurrentDirectory;
        return new PhysicalFileProvider(workingDirectory);
    }
}
