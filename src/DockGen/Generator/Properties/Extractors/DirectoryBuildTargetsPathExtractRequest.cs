using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record DirectoryBuildTargetsPathExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class DirectoryBuildTargetsPathExtractRequestHandler : IExtractRequestHandler<DirectoryBuildTargetsPathExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(DirectoryBuildTargetsPathExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryBuildTargets, out var importDirectoryBuildTargets) || importDirectoryBuildTargets == MSBuildProperties.False)
            {
                return ExtractResult<string>.Empty();
            }

            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryBuildTargetsPath, out var directoryBuildTargetsPath) && !string.IsNullOrEmpty(directoryBuildTargetsPath))
            {
                return ExtractResult<string>.Return(directoryBuildTargetsPath);
            }

            return ExtractResult<string>.Empty();
        }
    }
}