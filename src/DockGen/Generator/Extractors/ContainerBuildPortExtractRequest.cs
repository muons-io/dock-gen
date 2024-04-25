using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildPortExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBuildPortExtractRequestHandler : IExtractRequestHandler<ContainerBuildPortExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildPortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildPort, out var port) && !string.IsNullOrEmpty(port))
            {
                return ExtractResult<string>.Return(port);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}