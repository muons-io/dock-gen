namespace DockGen.Generator.Properties;

public interface IExtractRequestHandler<TRequest, TResponse>
    where TRequest : IExtractRequest<TResponse>
{
    ValueTask<ExtractResult<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}