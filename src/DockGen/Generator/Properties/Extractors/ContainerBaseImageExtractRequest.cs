using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBaseImageExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBaseImageExtractRequestHandler : IExtractRequestHandler<ContainerBaseImageExtractRequest, string>
    {
        private readonly IExtractor _extractor;

        public ContainerBaseImageExtractRequestHandler(IExtractor extractor)
        {
            _extractor = extractor;
        }

        public async ValueTask<ExtractResult<string>> Handle(ContainerBaseImageExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerBaseImage, out var image))
            {
                return ExtractResult<string>.Return(image);
            }

            var defaultBuildRegistry = DockGenConstants.DefaultBaseRegistry;
            var defaultBuildPort = DockGenConstants.DefaultBasePort;
            var defaultBuildRepository = DockGenConstants.DefaultBaseRepository;

            var targetFrameworkResult = await _extractor.ExtractAsync(new TargetFrameworkExtractRequest(request.Properties), cancellationToken);
            if (!targetFrameworkResult.Extracted)
            {
                return ExtractResult<string>.Empty();
            }

            var registryResult = await _extractor.ExtractAsync(new ContainerBaseRegistryExtractRequest(request.Properties), cancellationToken);
            var repositoryResult = await _extractor.ExtractAsync(new ContainerBaseRepositoryExtractRequest(request.Properties), cancellationToken);
            var portResult = await _extractor.ExtractAsync(new ContainerBasePortExtractRequest(request.Properties), cancellationToken);
            var tagResult = await _extractor.ExtractAsync(new ContainerBaseImageTagExtractRequest(request.Properties), cancellationToken);
            var familyResult = await _extractor.ExtractAsync(new ContainerBaseFamilyExtractRequest(request.Properties), cancellationToken);

            image = registryResult.Extracted ? registryResult.Value : defaultBuildRegistry;
            if (portResult.Extracted && !string.IsNullOrEmpty(portResult.Value))
            {
                image += $":{portResult.Value}";
            }
            else
            {
                image += $":{defaultBuildPort}";
            }

            image += repositoryResult.Extracted ? $"/{repositoryResult.Value}" : $"/{defaultBuildRepository}";
            if (tagResult.Extracted && !string.IsNullOrEmpty(tagResult.Value))
            {
                image += $":{tagResult.Value}";
            }

            if (familyResult.Extracted && !string.IsNullOrEmpty(familyResult.Value))
            {
                image += $"-{familyResult.Value}";
            }

            return ExtractResult<string>.Return(image);
        }
    }
}
