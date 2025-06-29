namespace DockGen.Generator;

public interface IDockGenAnalyser
{
    ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken);
}