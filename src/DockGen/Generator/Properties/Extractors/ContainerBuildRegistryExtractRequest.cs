using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildRegistryExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildRegistryExtractRequestHandler : IExtractRequestHandler<ContainerBuildRegistryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildRegistryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRegistry, out var registry) && !string.IsNullOrEmpty(registry))
            {
                return ExtractResult<string>.Return(registry);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
