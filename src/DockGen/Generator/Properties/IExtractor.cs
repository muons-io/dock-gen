namespace DockGen.Generator.Properties;

public interface IExtractor
{
    ValueTask<ExtractResult<TResponse>> ExtractAsync<TResponse>(IExtractRequest<TResponse> request, CancellationToken cancellationToken = default);
}
