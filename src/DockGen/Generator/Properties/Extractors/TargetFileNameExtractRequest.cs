using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record TargetFileNameExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class TargetFileNameExtractRequestHandler : IExtractRequestHandler<TargetFileNameExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetFileNameExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFileName, out var targetFileName) && !string.IsNullOrEmpty(targetFileName))
            {
                return ExtractResult<string>.Return(targetFileName);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
