using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildRegistryExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBuildRegistryExtractRequestHandler : IExtractRequestHandler<ContainerBuildRegistryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildRegistryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRegistry, out var registry) && !string.IsNullOrEmpty(registry))
            {
                return ExtractResult<string>.Return(registry);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}