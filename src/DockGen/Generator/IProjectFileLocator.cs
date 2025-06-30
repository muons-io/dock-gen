namespace DockGen.Generator;

public interface IProjectFileLocator
{
    /// <summary>
    /// Locates project files based on the provided request.
    /// </summary>
    /// <param name="request">The request containing the working directory and optional relative paths to locate project files.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of absolute paths for the located project files.</returns>
    Task<List<string>> LocateProjectFilesAsync(AnalyserRequest request, CancellationToken cancellationToken);
}
