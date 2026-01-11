using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DockGen;

public sealed class ExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ParseResult _parseResult;

    public ExecutionService(IServiceProvider serviceProvider, ParseResult parseResult)
    {
        _serviceProvider = serviceProvider;
        _parseResult = parseResult;
    }

    public async ValueTask<int> ExecuteAsync(InvocationConfiguration configuration, CancellationToken cancellationToken)
    {
        ExecutionContext.ServiceProvider = _serviceProvider;

        return await _parseResult.InvokeAsync(configuration, cancellationToken);
    }

    public static Func<ParseResult, CancellationToken, Task<int>> CommandHandlerAction<TCommand>() where TCommand : Command, new()
    {
        return (parseResult, cancellationToken) =>
        {
            var serviceProvider = ExecutionContext.ServiceProvider;
            if (serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider is not initialized.");
            }

            var scopedServiceProvider = serviceProvider.GetRequiredService<IServiceProvider>();
            var commandHandler = scopedServiceProvider.GetRequiredService<ICommandHandler<TCommand>>();
            return commandHandler.HandleAsync(parseResult, cancellationToken);
        };
    }
}
