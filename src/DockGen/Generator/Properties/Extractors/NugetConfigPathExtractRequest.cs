using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record NugetConfigPathExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class NugetConfigPathExtractRequestHandler : IExtractRequestHandler<NugetConfigPathExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(NugetConfigPathExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.RestoreConfigFile, out var nugetConfigPath) && !string.IsNullOrEmpty(nugetConfigPath))
            {
                return ExtractResult<string>.Return(nugetConfigPath);
            }

            return ExtractResult<string>.Empty();
        }
    }
}