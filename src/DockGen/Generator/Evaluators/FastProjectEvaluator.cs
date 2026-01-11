using System.Xml;
using DockGen.Generator.Constants;
using DockGen.Generator.Locators;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Generator.Evaluators;

public sealed class FastProjectEvaluator : IProjectEvaluator
{
    private static readonly string[] DirectoryBuildFileNames =
    [
        DockGenConstants.DirectoryBuildPropsFileName,
        DockGenConstants.DirectoryBuildTargetsFileName
    ];

    private static readonly string[] CentralManagementFileNames =
    [
        "Directory.Packages.props",
        "Packages.props"
    ];

    private readonly IFileProvider _fileProvider;
    private readonly IRelevantFileLocator _relevantFilesLocator;

    public FastProjectEvaluator(IFileProvider fileProvider, IRelevantFileLocator relevantFilesLocator)
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
        var projectPath = fileInfo.PhysicalPath;

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return new EvaluatedProject(
                Properties: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                References: [],
                RelevantFiles: []);
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return new EvaluatedProject(
                Properties: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                References: [],
                RelevantFiles: []);
        }

        var knownImportPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [MSBuildProperties.GeneralProperties.SolutionDir] = projectDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? projectDirectory
                : projectDirectory + Path.DirectorySeparatorChar,
            ["MSBuildProjectSdk"] = ReadProjectSdk(fileInfo)
        };

        AddDirectoryBuildFiles(projectDirectory, workingDirectory, knownImportPaths);
        AddCentralManagementFiles(projectDirectory, workingDirectory, knownImportPaths);

        var references = new List<string>();
        var importStack = new Stack<string>();

        foreach (var importPath in OrderImportsForEvaluation(knownImportPaths))
        {
            importStack.Push(importPath);
        }

        importStack.Push(relativeProjectPath);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (importStack.TryPop(out var currentPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!visited.Add(currentPath))
            {
                continue;
            }

            if (!TryOpenStreamForPath(currentPath, out var stream))
            {
                continue;
            }

            var physicalPath = _fileProvider.GetFileInfo(currentPath).PhysicalPath;
            if (string.IsNullOrWhiteSpace(physicalPath))
            {
                physicalPath = Path.GetFullPath(currentPath, workingDirectory);
            }

            using (stream)
            {
                ParseStream(physicalPath, stream, workingDirectory, properties, references, importStack);
            }
        }

        var relevantFiles = await _relevantFilesLocator.GetRelevantFilesAsync(projectPath, properties, cancellationToken);

        return new EvaluatedProject(
            Properties: properties,
            References: references,
            RelevantFiles: relevantFiles);
    }

    private static IEnumerable<string> OrderImportsForEvaluation(HashSet<string> importPaths)
    {
        var list = importPaths.ToList();

        list.Sort((left, right) =>
        {
            var leftIsTargets = left.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
            var rightIsTargets = right.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);

            if (leftIsTargets == rightIsTargets)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(left, right);
            }

            return leftIsTargets ? 1 : -1;
        });

        return list;
    }

    private void AddDirectoryBuildFiles(
        string projectDirectory,
        string workingDirectory,
        HashSet<string> importPaths)
    {
        foreach (var directory in EnumerateDirectoriesFromRootToLeaf(projectDirectory, workingDirectory))
        {
            foreach (var name in DirectoryBuildFileNames)
            {
                AddImportIfExists(directory, name, workingDirectory, importPaths);
            }
        }
    }

    private void AddCentralManagementFiles(
        string projectDirectory,
        string workingDirectory,
        HashSet<string> importPaths)
    {
        foreach (var directory in EnumerateDirectoriesFromRootToLeaf(projectDirectory, workingDirectory))
        {
            foreach (var name in CentralManagementFileNames)
            {
                AddImportIfExists(directory, name, workingDirectory, importPaths);
            }
        }
    }

    private void AddImportIfExists(
        string directory,
        string fileName,
        string workingDirectory,
        HashSet<string> importPaths)
    {
        var absoluteCandidate = Path.Combine(directory, fileName);
        var relativeCandidate = Path.GetRelativePath(workingDirectory, absoluteCandidate);

        relativeCandidate = relativeCandidate.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var providerFileInfo = _fileProvider.GetFileInfo(relativeCandidate);
        if (providerFileInfo.Exists)
        {
            importPaths.Add(relativeCandidate);
            return;
        }

        if (File.Exists(absoluteCandidate))
        {
            importPaths.Add(relativeCandidate);
        }
    }

    private static IEnumerable<string> EnumerateDirectoriesFromRootToLeaf(string projectDirectory, string workingDirectory)
    {
        var projectFullPath = Path.GetFullPath(projectDirectory);
        var workingFullPath = Path.GetFullPath(string.IsNullOrWhiteSpace(workingDirectory) ? projectDirectory : workingDirectory);

        var current = projectFullPath;
        var collected = new List<string>();

        while (!string.IsNullOrWhiteSpace(current))
        {
            collected.Add(current);

            if (PathsEqual(current, workingFullPath))
            {
                break;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        collected.Reverse();

        if (collected.Count == 0 || !PathsEqual(collected[0], workingFullPath))
        {
            collected.Insert(0, workingFullPath);
        }

        return collected;
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private bool TryOpenStreamForPath(string absoluteOrProviderPath, out Stream stream)
    {
        var fileInfo = _fileProvider.GetFileInfo(absoluteOrProviderPath);
        if (fileInfo.Exists)
        {
            stream = fileInfo.CreateReadStream();
            return true;
        }

        if (File.Exists(absoluteOrProviderPath))
        {
            stream = File.OpenRead(absoluteOrProviderPath);
            return true;
        }

        stream = Stream.Null;
        return false;
    }

    private static void ParseStream(
        string filePath,
        Stream stream,
        string workingDirectory,
        Dictionary<string, string> properties,
        List<string> projectReferences,
        Stack<string> importStack)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreWhitespace = true
        });

        while (reader.Read())
        {
            HandleElement(reader, filePath, workingDirectory, properties, projectReferences, importStack);
        }
    }

    private static void HandleElement(
        XmlReader reader,
        string filePath,
        string workingDirectory,
        Dictionary<string, string> properties,
        List<string> projectReferences,
        Stack<string> importStack)
    {
        if (reader.NodeType != XmlNodeType.Element)
        {
            return;
        }

        if (TryReadProjectReference(reader, projectReferences))
        {
            return;
        }

        if (TryReadImport(reader, filePath, workingDirectory, properties, importStack))
        {
            return;
        }

        if (reader.Depth >= 2)
        {
            return;
        }

        if (!reader.Name.Equals("PropertyGroup", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ReadPropertyGroup(reader, properties);
    }

    private static bool TryReadProjectReference(XmlReader reader, List<string> projectReferences)
    {
        if (!reader.Name.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var include = reader.GetAttribute("Include");
        if (!string.IsNullOrWhiteSpace(include))
        {
            include = include.Replace('\\', '/');
            projectReferences.Add(include);
        }

        return true;
    }

    private static bool TryReadImport(
        XmlReader reader,
        string filePath,
        string workingDirectory,
        Dictionary<string, string> properties,
        Stack<string> importStack)
    {
        if (!reader.Name.Equals("Import", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var project = reader.GetAttribute("Project");
        if (string.IsNullOrWhiteSpace(project))
        {
            return true;
        }

        var expandedProject = ExpandProperties(project, properties);
        if (string.IsNullOrWhiteSpace(expandedProject))
        {
            return true;
        }

        var resolvedPath = ResolveImportPath(filePath, expandedProject);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return true;
        }

        var relativePath = Path.GetRelativePath(workingDirectory, resolvedPath);
        importStack.Push(relativePath);

        return true;
    }

    private static void ReadPropertyGroup(XmlReader reader, Dictionary<string, string> properties)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        var groupDepth = reader.Depth;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement &&
                reader.Depth == groupDepth &&
                reader.Name.Equals("PropertyGroup", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.Depth != groupDepth + 1)
            {
                continue;
            }

            TryReadProperty(reader, groupDepth, properties);
        }
    }

    private static void TryReadProperty(XmlReader reader, int groupDepth, Dictionary<string, string> properties)
    {
        var propertyName = reader.Name;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        if (reader.IsEmptyElement)
        {
            return;
        }

        var propertyDepth = groupDepth + 1;

        if (!TryReadSimpleElementValue(reader, out var value))
        {
            SkipToEndOfElement(reader, propertyName, propertyDepth);
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        properties[propertyName] = ExpandProperties(value, properties);
    }

    private static bool TryReadSimpleElementValue(XmlReader reader, out string value)
    {
        value = string.Empty;

        if (reader.IsEmptyElement)
        {
            return true;
        }

        if (!reader.Read())
        {
            return false;
        }

        if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
        {
            value = reader.Value;
            return true;
        }

        if (reader.NodeType == XmlNodeType.EndElement)
        {
            return true;
        }

        return false;
    }

    private static void SkipToEndOfElement(XmlReader reader, string elementName, int elementDepth)
    {
        while (!(reader.NodeType == XmlNodeType.EndElement &&
                 reader.Depth == elementDepth &&
                 reader.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase)))
        {
            if (!reader.Read())
            {
                break;
            }
        }
    }

    private static string ResolveImportPath(string currentFilePath, string importProject)
    {
        if (Path.IsPathRooted(importProject))
        {
            return importProject;
        }

        var currentDirectory = Path.GetDirectoryName(currentFilePath);
        if (string.IsNullOrWhiteSpace(currentDirectory))
        {
            return importProject;
        }

        return Path.GetFullPath(Path.Combine(currentDirectory, importProject));
    }

    private static string ExpandProperties(string value, Dictionary<string, string> properties)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var current = value;

        for (var i = 0; i < 10; i++)
        {
            var next = ReplaceOnce(current, properties);
            if (string.Equals(next, current, StringComparison.Ordinal))
            {
                return next;
            }

            current = next;
        }

        return current;
    }

    private static string ReplaceOnce(string value, Dictionary<string, string> properties)
    {
        var start = value.IndexOf("$(", StringComparison.Ordinal);
        if (start < 0)
        {
            return value;
        }

        var end = value.IndexOf(')', start + 2);
        if (end < 0)
        {
            return value;
        }

        var name = value.Substring(start + 2, end - (start + 2));
        if (name.Length == 0)
        {
            return value;
        }

        if (!properties.TryGetValue(name, out var replacement))
        {
            replacement = string.Empty;
        }

        return string.Concat(
            value.AsSpan(0, start),
            replacement,
            value.AsSpan(end + 1));
    }

    private static string ReadProjectSdk(IFileInfo projectFile)
    {
        try
        {
            Stream stream;
            if (projectFile.Exists)
            {
                stream = projectFile.CreateReadStream();
            }
            else if (!string.IsNullOrWhiteSpace(projectFile.PhysicalPath) && File.Exists(projectFile.PhysicalPath))
            {
                stream = File.OpenRead(projectFile.PhysicalPath);
            }
            else
            {
                return string.Empty;
            }

            using (stream)
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true
            }))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    if (!reader.Name.Equals("Project", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return reader.GetAttribute("Sdk") ?? string.Empty;
                }
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }
}
