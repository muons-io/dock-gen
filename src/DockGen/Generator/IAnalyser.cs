namespace DockGen.Generator;

public interface IAnalyser
{
    ValueTask<List<Project>> AnalyseAsync(AnalyserRequest request, CancellationToken cancellationToken);
}