using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildImageTagExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBuildImageTagExtractRequestHandler : IExtractRequestHandler<ContainerBuildImageTagExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildImageTagExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImageTag, out var tag) && !string.IsNullOrEmpty(tag))
            {
                return ExtractResult<string>.Return(tag);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}