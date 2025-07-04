using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record ContainerBaseImageExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
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
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerBaseImage, out var image))
            {
                return ExtractResult<string>.Return(image);
            }

            var defaultBuildRegistry = Constants.Constants.DefaultBaseRegistry;
            var defaultBuildPort = Constants.Constants.DefaultBasePort;
            var defaultBuildRepository = Constants.Constants.DefaultBaseRepository;

            var targetFrameworkResult = await _extractor.ExtractAsync(new TargetFrameworkExtractRequest(request.AnalyzerResult), cancellationToken);
            if (!targetFrameworkResult.Extracted)
            {
                return ExtractResult<string>.Empty();
            }

            var registryResult = await _extractor.ExtractAsync(new ContainerBaseRegistryExtractRequest(request.AnalyzerResult), cancellationToken);
            var repositoryResult = await _extractor.ExtractAsync(new ContainerBaseRepositoryExtractRequest(request.AnalyzerResult), cancellationToken);
            var portResult = await _extractor.ExtractAsync(new ContainerBasePortExtractRequest(request.AnalyzerResult), cancellationToken);
            var tagResult = await _extractor.ExtractAsync(new ContainerBaseImageTagExtractRequest(request.AnalyzerResult), cancellationToken);
            var familyResult = await _extractor.ExtractAsync(new ContainerBaseFamilyExtractRequest(request.AnalyzerResult), cancellationToken);

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
