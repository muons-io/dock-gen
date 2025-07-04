using DockGen.Generator.PropertyExtractors.Constants;

namespace DockGen.Generator.PropertyExtractors.Extractors;

public sealed record OutputTypeExtractRequest(Project AnalyzerResult) : IExtractRequest<string>
{
    public sealed class OutputTypeExtractRequestHandler : IExtractRequestHandler<OutputTypeExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(OutputTypeExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.OutputType, out var outputType) && !string.IsNullOrEmpty(outputType))
            {
                return ExtractResult<string>.Return(outputType);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
