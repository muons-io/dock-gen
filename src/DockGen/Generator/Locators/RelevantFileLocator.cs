using DockGen.Generator.Constants;
using DockGen.Generator.Properties;
using DockGen.Generator.Properties.Extractors;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.Locators;

public sealed class RelevantFileLocator : IRelevantFileLocator
{
    private readonly IExtractor _extractor;
    private readonly IFileProvider _fileProvider;

    public RelevantFileLocator(IExtractor extractor, IFileProvider fileProvider)
    {
        this._extractor = extractor;
        _fileProvider = fileProvider;
    }


    public async Task<List<string>> GetRelevantFilesAsync(string absoluteProjectPath, Dictionary<string, string> properties, CancellationToken cancellationToken)
    {
        var relevantFiles = new List<string>();

        // directory build props file
        var directoryBuildPropsPathExtract = await _extractor.ExtractAsync(new DirectoryBuildPropsPathExtractRequest(properties), cancellationToken);
        if (directoryBuildPropsPathExtract.Extracted)
        {
            relevantFiles.Add(directoryBuildPropsPathExtract.Value);
        }
        else
        {
            var directoryBuildPropsPaths = FindAllFiles(DockGenConstants.DirectoryBuildPropsFileName, absoluteProjectPath, string.Empty);
            relevantFiles.AddRange(directoryBuildPropsPaths);
        }

        // directory build targets file
        var directoryBuildTargetsPathExtract = await _extractor.ExtractAsync(new DirectoryBuildTargetsPathExtractRequest(properties), cancellationToken);
        if (directoryBuildTargetsPathExtract.Extracted)
        {
            relevantFiles.Add(directoryBuildTargetsPathExtract.Value);
        }
        else
        {
            var directoryBuildTargetsPaths = FindAllFiles(DockGenConstants.DirectoryBuildTargetsFileName, absoluteProjectPath, string.Empty);
            relevantFiles.AddRange(directoryBuildTargetsPaths);
        }

        // directory packages props file
        var directoryPackagesPropsPathExtract = await _extractor.ExtractAsync(new DirectoryPackagesPropsPathExtractRequest(properties), cancellationToken);
        if (directoryPackagesPropsPathExtract.Extracted)
        {
            relevantFiles.Add(directoryPackagesPropsPathExtract.Value);
        }
        else
        {
            var directoryPackagesPropsPaths = FindAllFiles(DockGenConstants.DirectoryPackagesPropsFileName, absoluteProjectPath, string.Empty);
            relevantFiles.AddRange(directoryPackagesPropsPaths);
        }

        // nuget.config file
        var nugetConfigPathExtract = await _extractor.ExtractAsync(new NugetConfigPathExtractRequest(properties), cancellationToken);
        if (nugetConfigPathExtract.Extracted)
        {
            relevantFiles.Add(nugetConfigPathExtract.Value);
        }
        else
        {
            var nugetConfigPaths = FindAllFiles(DockGenConstants.NugetConfigFileName, absoluteProjectPath, string.Empty);
            relevantFiles.AddRange(nugetConfigPaths);
        }

        return relevantFiles;
    }

    private List<string> FindAllFiles(string fileName, string absoluteProjectPath, string relativeCurrentPath)
    {
        var filePaths = new List<string>();

        var items = _fileProvider.GetDirectoryContents(relativeCurrentPath);

        // we should only analyze directories that are parent to the project file
        foreach (var item in items)
        {
            if (!item.Exists)
            {
                continue;
            }

            if (item.IsDirectory)
            {
                var directoryPath = item.PhysicalPath!;

                if (Path.GetRelativePath(directoryPath, absoluteProjectPath).StartsWith("..", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var files = FindAllFiles(fileName, absoluteProjectPath, directoryPath);
                foreach (var filePath in files)
                {
                    filePaths.Add(filePath);
                }
            }

            if (fileName == item.Name)
            {
                filePaths.Add(item.PhysicalPath!);
            }
        }

        return filePaths;
    }
}
