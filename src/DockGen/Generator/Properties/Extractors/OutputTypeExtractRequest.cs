using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record OutputTypeExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class OutputTypeExtractRequestHandler : IExtractRequestHandler<OutputTypeExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(OutputTypeExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.OutputType, out var outputType) && !string.IsNullOrEmpty(outputType))
            {
                return ExtractResult<string>.Return(outputType);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
