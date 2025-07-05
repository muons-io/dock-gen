using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildImageTagExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildImageTagExtractRequestHandler : IExtractRequestHandler<ContainerBuildImageTagExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildImageTagExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImageTag, out var tag) && !string.IsNullOrEmpty(tag))
            {
                return ExtractResult<string>.Return(tag);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
