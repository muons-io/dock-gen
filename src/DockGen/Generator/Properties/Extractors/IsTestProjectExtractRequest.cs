using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record IsTestProjectExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<bool>
{
    public sealed class IsTestProjectExtractRequestHandler : IExtractRequestHandler<IsTestProjectExtractRequest, bool>
    {
        public ValueTask<ExtractResult<bool>> Handle(IsTestProjectExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.IsTestProject, out var isTestProject) && isTestProject == MSBuildProperties.True)
            {
                return ExtractResult<bool>.Return(true);
            }

            return ExtractResult<bool>.Empty();
        }
    }
}