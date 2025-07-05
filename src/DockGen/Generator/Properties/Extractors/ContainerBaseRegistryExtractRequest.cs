using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBaseRegistryExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBaseRegistryExtractRequestHandler : IExtractRequestHandler<ContainerBaseRegistryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseRegistryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRegistry, out var registry) && !string.IsNullOrEmpty(registry))
            {
                return ExtractResult<string>.Return(registry);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
