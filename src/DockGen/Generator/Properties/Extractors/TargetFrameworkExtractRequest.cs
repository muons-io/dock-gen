using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record TargetFrameworkExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class TargetFrameworkExtractRequestHandler : IExtractRequestHandler<TargetFrameworkExtractRequest, string>
    {
        public ValueTask<ExtractResult<string>> Handle(TargetFrameworkExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFramework, out var targetFramework) && !string.IsNullOrEmpty(targetFramework))
            {
                return ExtractResult<string>.Return(targetFramework);
            }

            return ExtractResult<string>.Empty();
        }
    }
}
