using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBuildPortExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
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
