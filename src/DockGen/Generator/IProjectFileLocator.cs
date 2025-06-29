namespace DockGen.Generator;

public interface IProjectFileLocator
{
    Task<List<string>> LocateProjectFilesAsync(AnalyserRequest request, CancellationToken cancellationToken);
}