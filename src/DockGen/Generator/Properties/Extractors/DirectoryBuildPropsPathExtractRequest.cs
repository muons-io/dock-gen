using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record DirectoryBuildPropsPathExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class DirectoryBuildPropsPathExtractRequestHandler : IExtractRequestHandler<DirectoryBuildPropsPathExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(DirectoryBuildPropsPathExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.ImportDirectoryBuildProps, out var importDirectoryBuildProps) || importDirectoryBuildProps == MSBuildProperties.False)
            {
                return ExtractResult<string>.Empty();
            }

            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.DirectoryBuildPropsPath, out var directoryBuildPropsPath) && !string.IsNullOrEmpty(directoryBuildPropsPath))
            {
                return ExtractResult<string>.Return(directoryBuildPropsPath);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
