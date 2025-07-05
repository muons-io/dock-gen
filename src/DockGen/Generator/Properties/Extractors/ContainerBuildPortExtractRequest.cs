using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildPortExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildPortExtractRequestHandler : IExtractRequestHandler<ContainerBuildPortExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBuildPortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildPort, out var port) && !string.IsNullOrEmpty(port))
            {
                return ExtractResult<string>.Return(port);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
