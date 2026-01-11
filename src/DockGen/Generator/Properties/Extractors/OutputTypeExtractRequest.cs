using DockGen.Generator.Constants;

namespace DockGen.Generator.Properties.Extractors;

public sealed record OutputTypeExtractRequest(Dictionary<string, string> Properties) : IExtractRequest<string>
{
    public sealed class OutputTypeExtractRequestHandler : IExtractRequestHandler<OutputTypeExtractRequest, string>
    {
        private static readonly string[] WebSdkMarkers =
        {
            "Microsoft.NET.Sdk.Web",
            "Microsoft.NET.Sdk.Worker"
        };

        public ValueTask<ExtractResult<string>> Handle(OutputTypeExtractRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Properties.TryGetValue(MSBuildProperties.GeneralProperties.OutputType, out var outputType) && !string.IsNullOrEmpty(outputType))
            {
                return ExtractResult<string>.Return(outputType);
            }

            if (TryGetSdk(request.Properties, out var sdk))
            {
                if (WebSdkMarkers.Any(marker => sdk.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                {
                    return ExtractResult<string>.Return("Exe");
                }
            }

            return ExtractResult<string>.Empty();
        }

        private static bool TryGetSdk(Dictionary<string, string> properties, out string sdk)
        {
            if (properties.TryGetValue("MSBuildProjectSdk", out sdk!) && !string.IsNullOrWhiteSpace(sdk))
            {
                return true;
            }

            if (properties.TryGetValue("ProjectSdk", out sdk!) && !string.IsNullOrWhiteSpace(sdk))
            {
                return true;
            }

            sdk = string.Empty;
            return false;
        }
    }
}
