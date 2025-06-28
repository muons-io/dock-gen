using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record ContainerBuildImageExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
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
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImage, out var image))
            {
                return ExtractResult<string>.Return(image);
            }

            var defaultBuildRegistry = Constants.Constants.DefaultBuildRegistry;
            var defaultBuildPort = Constants.Constants.DefaultBuildPort;
            var defaultBuildRepository = Constants.Constants.DefaultBuildRepository;

            var targetFrameworkResult = await _extractor.ExtractAsync(new TargetFrameworkExtractRequest(request.AnalyzerResult), cancellationToken);
            if (!targetFrameworkResult.Extracted)
            {
                return ExtractResult<string>.Empty();
            }

            var registryResult = await _extractor.ExtractAsync(new ContainerBuildRegistryExtractRequest(request.AnalyzerResult), cancellationToken);
            var repositoryResult = await _extractor.ExtractAsync(new ContainerBuildRepositoryExtractRequest(request.AnalyzerResult), cancellationToken);
            var portResult = await _extractor.ExtractAsync(new ContainerBuildPortExtractRequest(request.AnalyzerResult), cancellationToken);
            var tagResult = await _extractor.ExtractAsync(new ContainerBuildImageTagExtractRequest(request.AnalyzerResult), cancellationToken);
            var familyResult = await _extractor.ExtractAsync(new ContainerBuildFamilyExtractRequest(request.AnalyzerResult), cancellationToken);

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

