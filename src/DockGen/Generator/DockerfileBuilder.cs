using System.Text;

namespace DockGen.Generator;

public sealed class DockerfileBuilder
{
    public required string BuildImage { get; init; }
    public required string BaseImage { get; init; }
    public required string WorkDir { get; init; }
    
    public required string ProjectDirectory { get; init; }
    public required string ProjectFile { get; init; }

    public Dictionary<string, string> InitialCopy { get; set; } = new();
    public Dictionary<string, string> Copy { get; set; } = new();
    public required string TargetFileName { get; init; }

    public string Build()
    {
        var sb = new StringBuilder();
        
        sb = BuildBaseLayer(sb);
        sb = BuildBuildLayer(sb);
        sb = BuildPublishLayer(sb);
        sb = BuildFinalLayer(sb);
        
        return sb.ToString();
    }

    private StringBuilder BuildBaseLayer(StringBuilder sb)
    {
        sb.AppendLine($"FROM {BaseImage} AS base");
        sb.AppendLine($"WORKDIR {NormalizeDirectoryPath(WorkDir)}");
        sb.AppendLine();
        
        return sb;
    }
    
    private StringBuilder BuildBuildLayer(StringBuilder sb)
    {
        sb.AppendLine($"FROM {BuildImage} AS build");
        sb.AppendLine("WORKDIR /src");
        sb.AppendLine();
        
        foreach (var (source, destination) in InitialCopy.OrderBy(x => x.Key))
        {
            sb.AppendLine($"COPY [\"{NormalizeFilePath(source)}\", \"{NormalizeDirectoryPath(destination)}\"]");
        }
        
        foreach (var (source, destination) in Copy.OrderBy(x => x.Key))
        {
            sb.AppendLine($"COPY [\"{NormalizeFilePath(source)}\", \"{NormalizeDirectoryPath(destination)}\"]");
        }
        
        var relativeProjectPath = Path.Combine(ProjectDirectory, ProjectFile);
        
        sb.AppendLine($"RUN dotnet restore \"{NormalizeFilePath(relativeProjectPath)}\"");
        sb.AppendLine("COPY . .");
        
        var workDir = Path.Combine("/src", ProjectDirectory);
        sb.AppendLine($"WORKDIR \"{NormalizeDirectoryPath(workDir)}\"");
        sb.AppendLine($"RUN dotnet build \"{NormalizeFilePath(ProjectFile)}\" -c Release -o /app/build");
        sb.AppendLine();
        
        return sb;
    }

    private StringBuilder BuildPublishLayer(StringBuilder sb)
    {
        sb.AppendLine("FROM build AS publish");
        sb.AppendLine($"RUN dotnet publish \"{NormalizeFilePath(ProjectFile)}\" -c Release -o /app/publish");
        sb.AppendLine();
        
        return sb;
    }

    private StringBuilder BuildFinalLayer(StringBuilder sb)
    {
        sb.AppendLine("FROM base AS final");
        sb.AppendLine($"WORKDIR {NormalizeDirectoryPath(WorkDir)}");
        sb.AppendLine("COPY --from=publish /app/publish .");
        sb.AppendLine();
        sb.AppendLine($"ENTRYPOINT [\"dotnet\", \"{TargetFileName}\"]");
        
        return sb;
    }
    
    private static string NormalizeFilePath(string path)
    {
        return path.Replace("\\", "/");
    }
    
    private static string NormalizeDirectoryPath(string path)
    {
        var normalizedPath = NormalizeFilePath(path);
        if (!normalizedPath.EndsWith('/'))
        {
            normalizedPath += "/";
        }
        
        return normalizedPath;
    }
}