using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBuildRegistryExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
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
