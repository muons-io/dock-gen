using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DockGen;

public sealed class CommandHostBuilder
{
    private readonly IServiceCollection _services;

    public RootCommand RootCommand { get; }

    public CommandHostBuilder(IServiceCollection services, RootCommand rootCommand)
    {
        _services = services;
        RootCommand = rootCommand;
    }

    public void Build(string[] args)
    {
        var parseResult = RootCommand.Parse(args);
        _services.AddSingleton(parseResult);
    }

    public CommandHostBuilder AddCommand<TCommand, TCommandHandler>()
        where TCommand : Command, new()
        where TCommandHandler : class, ICommandHandler<TCommand>
    {
        _services.AddTransient<ICommandHandler<TCommand>, TCommandHandler>();

        var command = new TCommand();
        command.SetAction(ExecutionService.CommandHandlerAction<TCommand>());

        RootCommand.Add(command);

        return this;
    }
}
