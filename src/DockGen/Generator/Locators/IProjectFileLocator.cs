namespace DockGen.Generator.Locators;

public interface IProjectFileLocator
{
    /// <summary>
    /// Locates project files based on the provided request.
    /// </summary>
    /// <param name="request">The request containing the working directory and optional relative paths to locate project files.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>List of absolute paths for the located project files.</returns>
    Task<List<string>> LocateProjectFilesAsync(AnalyzerRequest request, CancellationToken cancellationToken);
}
