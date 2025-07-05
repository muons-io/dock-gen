using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace DockGen.Generator.Locators;

public sealed class ProjectFileLocator : IProjectFileLocator
{
    private readonly ILogger<ProjectFileLocator> _logger;
    private readonly IFileProvider _fileProvider;

    public ProjectFileLocator(ILogger<ProjectFileLocator> logger, IFileProvider fileProvider)
    {
        _logger = logger;
        _fileProvider = fileProvider;
    }

    public async Task<List<string>> LocateProjectFilesAsync(AnalyzerRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Locating project files for request: {Request}", request);

        var projectFiles = (request.RelativeDirectory, request.RelativeSolutionPath, request.RelativeProjectPath) switch
        {
            (_, _, not null) => FindProject(request.RelativeProjectPath),
            (_, not null, _) => await FindProjectsInSolutionAsync(request.RelativeSolutionPath, cancellationToken),
            (not null, _, _) => FindProjectsInDirectory(request.WorkingDirectory, request.RelativeDirectory!),
            _ => FindProjectsInDirectory(request.WorkingDirectory, ".")
        };

        if (projectFiles.Count == 0)
        {
            _logger.LogWarning("No project files found in the specified paths");
            return [];
        }

        _logger.LogInformation("Found {ProjectCount} project files to analyze", projectFiles.Count);
        return projectFiles;
    }

    private List<string> FindProject(string relativeProjectPath)
    {
        var projectFiles = new List<string>();

        var projectFile = _fileProvider.GetFileInfo(relativeProjectPath);
        if (projectFile.Exists && !projectFile.IsDirectory)
        {
            projectFiles.Add(projectFile.PhysicalPath!);
        }

        return projectFiles;
    }

    private async Task<List<string>> FindProjectsInSolutionAsync(string solutionPath, CancellationToken ct = default)
    {
        var supportedExtensions = new[] { ".sln", ".slnx" };

        var projectFiles = new List<string>();

        var solutionFile = _fileProvider.GetFileInfo(solutionPath);
        if (!solutionFile.Exists || solutionFile.IsDirectory)
        {
            _logger.LogError("Solution file {SolutionPath} does not exist", solutionPath);
            return projectFiles;
        }

        var extension = Path.GetExtension(solutionFile.PhysicalPath!);
        if (!supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogError("Solution file {SolutionPath} is not a supported solution file type", solutionPath);
            return projectFiles;
        }

        var serializer = SolutionSerializers.GetSerializerByMoniker(extension);
        if (serializer is null)
        {
            _logger.LogError("No serializer found for solution file {SolutionPath}", solutionPath);
            return projectFiles;
        }

        var solutionFileDirectory = Path.GetDirectoryName(solutionFile.PhysicalPath!);
        var solution = await serializer.OpenAsync(solutionFile.PhysicalPath!, ct);
        foreach (var project in solution.SolutionProjects)
        {
            var absoluteProjectPath = Path.GetFullPath(project.FilePath, solutionFileDirectory!);
            projectFiles.Add(absoluteProjectPath);
        }

        return projectFiles;
    }

    private List<string> FindProjectsInDirectory(string workingDirectory, string relativeDirectory)
    {
        var projectFiles = new List<string>();

        var items = _fileProvider.GetDirectoryContents(relativeDirectory);
        foreach(var item in items)
        {
            if (item.IsDirectory)
            {
                // Recursively get project files from subdirectories
                var relativePath = Path.GetRelativePath(workingDirectory, item.PhysicalPath!);
                var subProjectFiles = FindProjectsInDirectory(workingDirectory, relativePath);
                projectFiles.AddRange(subProjectFiles);
                continue;
            }

            var extension = Path.GetExtension(item.PhysicalPath!);
            if (!extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            projectFiles.Add(item.PhysicalPath!);
        }

        return projectFiles;
    }
}
