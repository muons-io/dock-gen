using System.CommandLine;

namespace DockGen;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler where TCommand :  notnull, new()
{
    Type CommandType => typeof(TCommand);
    Task<int> HandleAsync(ParseResult parseResult, CancellationToken cancellationToken = default);
}
