using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBaseImageTagExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBaseImageTagExtractRequestHandler : IExtractRequestHandler<ContainerBaseImageTagExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBaseImageTagExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerImageTag, out var tag) && !string.IsNullOrEmpty(tag))
            {
                return ExtractResult<string>.Return(tag);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
