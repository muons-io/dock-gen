using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace DockGen.Generator;

public sealed class ProjectFileLocator : IProjectFileLocator
{
    private readonly ILogger<ProjectFileLocator> _logger;
    private readonly IFileProvider _fileProvider;

    public ProjectFileLocator(ILogger<ProjectFileLocator> logger, IFileProvider fileProvider)
    {
        _logger = logger;
        _fileProvider = fileProvider;
    }

    public async Task<List<string>> LocateProjectFilesAsync(AnalyserRequest request, CancellationToken cancellationToken)
    {
        var workingDirectory = _fileProvider.GetFileInfo(request.WorkingDirectory);
        if (!workingDirectory.Exists || !workingDirectory.IsDirectory)
        {
            _logger.LogError("Working directory {WorkingDirectory} does not exist or is not a directory", request.WorkingDirectory);
            return [];
        }

        var projectFiles = (request.WorkingDirectory, request.SolutionPath, request.ProjectPath) switch
        {
            (_, _, not null) => FindProject(request.ProjectPath),
            (_, not null, _) => await FindProjectsInSolutionAsync(request.SolutionPath, cancellationToken),
            _ => FindProjectsInDirectory(workingDirectory.PhysicalPath!)
        };

        if (projectFiles.Count == 0)
        {
            _logger.LogWarning("No project files found in the specified paths");
            return [];
        }

        _logger.LogInformation("Found {ProjectCount} project files to analyze", projectFiles.Count);
        return projectFiles;
    }

    private List<string> FindProject(string projectPath)
    {
        var projectFiles = new List<string>();

        var projectFile = _fileProvider.GetFileInfo(projectPath);
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

        var solution = await serializer.OpenAsync(solutionFile.PhysicalPath!, ct);
        foreach (var project in solution.SolutionProjects)
        {
            projectFiles.Add(project.FilePath);
        }

        return projectFiles;
    }

    private List<string> FindProjectsInDirectory(string workingDirectory)
    {
        var projectFiles = new List<string>();

        var items = _fileProvider.GetDirectoryContents(workingDirectory);
        foreach(var item in items)
        {
            if (item.IsDirectory)
            {
                // Recursively get project files from subdirectories
                var subProjectFiles = FindProjectsInDirectory(item.PhysicalPath!);
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
