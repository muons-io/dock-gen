using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildFamilyExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBuildFamilyExtractRequestHandler : IExtractRequestHandler<ContainerBuildFamilyExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildFamilyExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildFamily, out var family) && !string.IsNullOrEmpty(family))
            {
                return ExtractResult<string>.Return(family);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
