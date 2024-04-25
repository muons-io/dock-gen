using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record TargetFileNameExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class TargetFileNameExtractRequestHandler : IExtractRequestHandler<TargetFileNameExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetFileNameExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFileName, out var targetFileName) && !string.IsNullOrEmpty(targetFileName))
            {
                return ExtractResult<string>.Return(targetFileName);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}