using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record ContainerBuildImageExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class ContainerBuildImageExtractRequestHandler : IExtractRequestHandler<ContainerBuildImageExtractRequest, string>
    {
        private readonly IExtractor _extractor;

        public ContainerBuildImageExtractRequestHandler(IExtractor extractor)
        {
            _extractor = extractor;
        }

        public async ValueTask<ExtractResult<string>> Handle(ContainerBuildImageExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImage, out var image))
            {
                return ExtractResult<string>.Return(image);
            }

            var defaultBuildRegistry = Constants.DockGenConstants.DefaultBuildRegistry;
            var defaultBuildPort = Constants.DockGenConstants.DefaultBuildPort;
            var defaultBuildRepository = Constants.DockGenConstants.DefaultBuildRepository;

            var targetFrameworkResult = await _extractor.ExtractAsync(new TargetFrameworkExtractRequest(request.Properties), cancellationToken);
            if (!targetFrameworkResult.Extracted)
            {
                return ExtractResult<string>.Empty();
            }

            var registryResult = await _extractor.ExtractAsync(new ContainerBuildRegistryExtractRequest(request.Properties), cancellationToken);
            var repositoryResult = await _extractor.ExtractAsync(new ContainerBuildRepositoryExtractRequest(request.Properties), cancellationToken);
            var portResult = await _extractor.ExtractAsync(new ContainerBuildPortExtractRequest(request.Properties), cancellationToken);
            var tagResult = await _extractor.ExtractAsync(new ContainerBuildImageTagExtractRequest(request.Properties), cancellationToken);
            var familyResult = await _extractor.ExtractAsync(new ContainerBuildFamilyExtractRequest(request.Properties), cancellationToken);

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

