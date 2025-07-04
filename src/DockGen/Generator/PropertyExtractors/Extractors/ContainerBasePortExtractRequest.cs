using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBasePortExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBasePortExtractRequestHandler : IExtractRequestHandler<ContainerBasePortExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBasePortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBasePort, out var port) && !string.IsNullOrEmpty(port))
            {
                return ExtractResult<string>.Return(port);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
