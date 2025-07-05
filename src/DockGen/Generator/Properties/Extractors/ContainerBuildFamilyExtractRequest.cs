using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildFamilyExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildFamilyExtractRequestHandler : IExtractRequestHandler<ContainerBuildFamilyExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildFamilyExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildFamily, out var family) && !string.IsNullOrEmpty(family))
            {
                return ExtractResult<string>.Return(family);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
