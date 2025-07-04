using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBaseRepositoryExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class ContainerBaseRepositoryExtractRequestHandler : IExtractRequestHandler<ContainerBaseRepositoryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseRepositoryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRepository, out var repository) && !string.IsNullOrEmpty(repository))
            {
                return ExtractResult<string>.Return(repository);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
