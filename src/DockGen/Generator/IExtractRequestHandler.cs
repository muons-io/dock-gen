namespace DockGen.Generator;

public interface IExtractRequestHandler<TRequest, TResponse>
    where TRequest : IExtractRequest<TResponse>
{
    ValueTask<ExtractResult<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}