using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildRepositoryExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBuildRepositoryExtractRequestHandler : IExtractRequestHandler<ContainerBuildRepositoryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildRepositoryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRepository, out var repository) && !string.IsNullOrEmpty(repository))
            {
                return ExtractResult<string>.Return(repository);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}