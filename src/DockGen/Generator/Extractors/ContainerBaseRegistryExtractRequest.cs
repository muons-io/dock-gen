using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBaseRegistryExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBaseRegistryExtractRequestHandler : IExtractRequestHandler<ContainerBaseRegistryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseRegistryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRegistry, out var registry) && !string.IsNullOrEmpty(registry))
            {
                return ExtractResult<string>.Return(registry);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
