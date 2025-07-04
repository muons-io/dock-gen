using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBaseImageTagExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBaseImageTagExtractRequestHandler : IExtractRequestHandler<ContainerBaseImageTagExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseImageTagExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerImageTag, out var tag) && !string.IsNullOrEmpty(tag))
            {
                return ExtractResult<string>.Return(tag);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
