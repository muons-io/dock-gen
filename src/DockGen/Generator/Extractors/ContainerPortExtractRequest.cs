using DockGen.Constants;
using DockGen.Generator.Models;

namespace DockGen.Generator.Extractors;

public sealed record ContainerPortExtractRequest(Project AnalyzerResult) : IExtractRequest<List<ContainerPort>>
{
    public sealed class ContainerPortExtractRequestHandler : IExtractRequestHandler<ContainerPortExtractRequest, List<ContainerPort>>
    {
        public ValueTask<ExtractResult<List<ContainerPort>>> Handle(ContainerPortExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.AnalyzerResult.Items.TryGetValue(MSBuildProperties.ContainerProperties.ContainerPort, out var ports) || !ports.Any())
            {
                return ExtractResult<List<ContainerPort>>.Empty();
            }

            var containerPorts = new List<ContainerPort>();
            foreach (var port in ports)
            {
                var type = port.Metadata.TryGetValue(MSBuildProperties.ContainerMetadata.ContainerPortType, out var typeValue) ? typeValue : Constants.Constants.DefaultContainerPortType;
                var value = port.ItemSpec;

                containerPorts.Add(new ContainerPort(value, type));
            }

            return ExtractResult<List<ContainerPort>>.Return(containerPorts);
        }
    }
}
