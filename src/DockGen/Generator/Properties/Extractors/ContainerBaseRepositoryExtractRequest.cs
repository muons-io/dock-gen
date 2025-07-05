using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBaseRepositoryExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBaseRepositoryExtractRequestHandler : IExtractRequestHandler<ContainerBaseRepositoryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseRepositoryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRepository, out var repository) && !string.IsNullOrEmpty(repository))
            {
                return ExtractResult<string>.Return(repository);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
