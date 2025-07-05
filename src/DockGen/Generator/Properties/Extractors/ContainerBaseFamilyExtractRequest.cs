using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBaseFamilyExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBaseFamilyExtractRequestHandler : IExtractRequestHandler<ContainerBaseFamilyExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseFamilyExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerFamily, out var family) && !string.IsNullOrEmpty(family))
            {
                return ExtractResult<string>.Return(family);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
