namespace DockGen.Generator.Locators;

public interface IRelevantFileLocator
{
    Task<List<string>> GetRelevantFilesAsync(string absoluteProjectPath, Dictionary<string, string> properties, CancellationToken cancellationToken);
}
