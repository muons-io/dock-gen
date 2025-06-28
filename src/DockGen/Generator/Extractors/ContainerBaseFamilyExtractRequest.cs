using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBaseFamilyExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBaseFamilyExtractRequestHandler : IExtractRequestHandler<ContainerBaseFamilyExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseFamilyExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerFamily, out var family) && !string.IsNullOrEmpty(family))
            {
                return ExtractResult<string>.Return(family);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
