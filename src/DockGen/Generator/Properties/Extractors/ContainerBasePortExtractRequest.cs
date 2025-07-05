using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBasePortExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBasePortExtractRequestHandler : IExtractRequestHandler<ContainerBasePortExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(ContainerBasePortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBasePort, out var port) && !string.IsNullOrEmpty(port))
            {
                return ExtractResult<string>.Return(port);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
