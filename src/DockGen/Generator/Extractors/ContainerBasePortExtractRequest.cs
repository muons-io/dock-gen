using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBasePortExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBasePortExtractRequestHandler : IExtractRequestHandler<ContainerBasePortExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBasePortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerPort, out var port) && !string.IsNullOrEmpty(port))
            {
                return ExtractResult<string>.Return(port);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}