using System.Xml;
using DockGen.Generator.Constants;
using DockGen.Generator.Locators;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.Evaluators;

/// <summary>
/// Bit naive project evaluator that reads the project file and extracts properties and references.
/// </summary>
public sealed class SimpleProjectEvaluator : IProjectEvaluator
{
    private static readonly string[] ReadPropertiesFrom =
    [
        DockGenConstants.DirectoryBuildPropsFileName,
        DockGenConstants.DirectoryBuildTargetsFileName
    ];

    private readonly IFileProvider _fileProvider;
    private readonly IRelevantFileLocator _relevantFilesLocator;

    public SimpleProjectEvaluator(IFileProvider fileProvider, IRelevantFileLocator relevantFilesLocator)
    {
        _fileProvider = fileProvider;
        _relevantFilesLocator = relevantFilesLocator;
    }

    public async Task<EvaluatedProject> EvaluateAsync(
        string workingDirectory,
        string relativeProjectPath,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = _fileProvider.GetFileInfo(relativeProjectPath);

        var globalProperties = FindAllGlobalProperties(fileInfo.PhysicalPath!, string.Empty);

        await using var stream = fileInfo.CreateReadStream();
        using var xmlReader = XmlReader.Create(stream);
        var projectOptions = new ProjectOptions
        {
            GlobalProperties = globalProperties
        };

        var p = Microsoft.Build.Evaluation.Project.FromXmlReader(xmlReader, projectOptions);
        var projectReferences = p.Items
            .Where(x => x.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.EvaluatedInclude)
            .ToList();

        var projectProperties = p.Properties
            .ToDictionary(x => x.Name, x => x.EvaluatedValue, StringComparer.OrdinalIgnoreCase);

        var relevantFiles = await _relevantFilesLocator.GetRelevantFilesAsync(fileInfo.PhysicalPath!, projectProperties, cancellationToken);

        return new EvaluatedProject(
            Properties: projectProperties,
            References: projectReferences,
            RelevantFiles: relevantFiles);
    }

    /// <summary>
    /// Finds all global properties for a project by recursively scanning directories for Directory.Build.props and Directory.Build.targets files.
    /// Properties from files higher in the directory tree will be overwritten by those lower in the tree.
    /// </summary>
    /// <param name="absoluteProjectPath">The absolute path to the project file. Used to determine which directories to analyze.</param>
    /// <param name="relativeCurrentPath">The current path being analyzed in the recursive search.</param>
    /// <returns>A dictionary containing global properties as key-value pairs found in build property files.</returns>
    private Dictionary<string, string> FindAllGlobalProperties(string absoluteProjectPath, string relativeCurrentPath)
    {
        var globalProperties = new Dictionary<string, string>();

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

                // check if the directory is a parent of the project file
                if (Path.GetRelativePath(directoryPath, absoluteProjectPath).StartsWith("..", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var buildProperties = FindAllGlobalProperties(absoluteProjectPath, directoryPath);
                foreach (var property in buildProperties)
                {
                    globalProperties[property.Key] = property.Value;
                }
            }

            if (ReadPropertiesFrom.Contains(item.Name))
            {
                var buildProperties = ReadPropertiesFromFile(item);
                foreach (var property in buildProperties)
                {
                    globalProperties[property.Key] = property.Value;
                }
            }
        }

        return globalProperties;
    }

    private Dictionary<string, string> ReadPropertiesFromFile(IFileInfo fileInfo)
    {
        var properties = new Dictionary<string, string>();

        using var stream = fileInfo.CreateReadStream();
        using var xmlReader = XmlReader.Create(stream);
        var projectRootElement = ProjectRootElement.Create(xmlReader);
        foreach (var property in projectRootElement.Properties)
        {
            properties[property.Name] = property.Value;
        }

        return properties;
    }
}
