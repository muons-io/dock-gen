using Buildalyzer;
using DockGen.Constants;

namespace DockGen.Generator.Extractors;

public sealed record TargetFrameworkExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class TargetFrameworkExtractRequestHandler : IExtractRequestHandler<TargetFrameworkExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetFrameworkExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFramework, out var targetFramework) && !string.IsNullOrEmpty(targetFramework))
            {
                return ExtractResult<string>.Return(targetFramework);
            }
            
            return ExtractResult<string>.Empty();
        }
    }
}