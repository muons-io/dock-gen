namespace DockGen.Generator;

public interface IAnalyzer
{
    ValueTask<List<Project>> AnalyseAsync(AnalyzerRequest request, CancellationToken cancellationToken);
}
