using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.ProjectEvaluators;

/// <summary>
/// Bit naive project evaluator that reads the project file and extracts properties and references.
/// </summary>
public sealed class SimpleProjectEvaluator : IProjectEvaluator
{
    private readonly IFileProvider _fileProvider;

    public SimpleProjectEvaluator(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public async Task<EvaluatedProject> EvaluateAsync(
        string workingDirectory,
        string relativeProjectPath,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = _fileProvider.GetFileInfo(relativeProjectPath);

        var globalProperties = FindAllGlobalProperties(string.Empty);

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

        return new EvaluatedProject(
            Properties: projectProperties,
            References: projectReferences);
    }

    /// <summary>
    /// Finds all global properties for a project based on Directory.Build.props and Directory.Build.targets files.
    /// </summary>
    /// <param name="relativeCurrentPath">The current path being analyzed.</param>
    /// <returns>A dictionary containing global properties as key-value pairs.</returns>
    private Dictionary<string, string> FindAllGlobalProperties(string relativeCurrentPath)
    {
        string[] readPropertiesFrom =
        [
            "Directory.Build.props",
            "Directory.Build.targets"
        ];

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

                var buildProperties = FindAllGlobalProperties(directoryPath);
                foreach (var property in buildProperties)
                {
                    globalProperties[property.Key] = property.Value;
                }
            }

            if (readPropertiesFrom.Contains(item.Name))
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
