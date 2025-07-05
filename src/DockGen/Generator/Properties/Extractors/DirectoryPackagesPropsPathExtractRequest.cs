using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record DirectoryPackagesPropsPathExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class DirectoryPackagesPropsPathExtractRequestHandler : IExtractRequestHandler<DirectoryPackagesPropsPathExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(DirectoryPackagesPropsPathExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryPackagesProps, out var importDirectoryPackagesProps) || importDirectoryPackagesProps == MSBuildProperties.False)
            {
                return ExtractResult<string>.Empty();
            }

            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryPackagesPropsPath, out var directoryPackagesPropsPath) && !string.IsNullOrEmpty(directoryPackagesPropsPath))
            {
                return ExtractResult<string>.Return(directoryPackagesPropsPath);
            }

            return ExtractResult<string>.Empty();
        }
    }
}