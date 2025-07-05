using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildRepositoryExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildRepositoryExtractRequestHandler : IExtractRequestHandler<ContainerBuildRepositoryExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildRepositoryExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRepository, out var repository) && !string.IsNullOrEmpty(repository))
            {
                return ExtractResult<string>.Return(repository);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
