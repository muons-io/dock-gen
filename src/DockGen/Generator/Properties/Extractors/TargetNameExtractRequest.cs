using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record TargetNameExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class TargetNameExtractRequestHandler : IExtractRequestHandler<TargetNameExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetNameExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetName, out var targetFileName) && !string.IsNullOrEmpty(targetFileName))
            {
                return ExtractResult<string>.Return(targetFileName);
            }

            return ExtractResult<string>.Empty();
        }
    }
}

public sealed record TargetExtExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class TargetExtExtractRequestHandler : IExtractRequestHandler<TargetExtExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetExtExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetExt, out var targetFileExt) && !string.IsNullOrEmpty(targetFileExt))
            {
                return ExtractResult<string>.Return(targetFileExt);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
