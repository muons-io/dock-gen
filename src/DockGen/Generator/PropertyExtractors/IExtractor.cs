using Microsoft.Extensions.DependencyInjection;

namespace DockGen.Generator.PropertyExtractors;

public interface IExtractor
{
    ValueTask<ExtractResult<TResponse>> ExtractAsync<TResponse>(IExtractRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public sealed class Extractor : IExtractor
{
    private readonly IServiceProvider _serviceProvider;

    public Extractor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ValueTask<ExtractResult<TResponse>> ExtractAsync<TResponse>(IExtractRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IExtractRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IExtractRequestHandler<IExtractRequest<TResponse>, TResponse>.Handle))!;
        return (ValueTask<ExtractResult<TResponse>>)method.Invoke(handler, new object[] { request, cancellationToken })!;
    }
}
