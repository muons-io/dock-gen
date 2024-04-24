using System.Diagnostics.CodeAnalysis;
using Buildalyzer;
using DockGen.Constants;
using MediatR;

namespace DockGen.Generator;

public static class DockerfileBuilderHelpers
{
    /// <summary>
    /// Getting the build image with the convention: REGISTRY[:PORT]/REPOSITORY[:TAG[-FAMILY]]
    /// </summary>
    public static bool TryGetBuildImage(IAnalyzerResult analyzerResult, [NotNullWhen(true)] out string? image)
    {
        image = null;
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImage, out image))
        {
            return true;
        }
        
        if (!analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFramework, out var targetFramework))
        {
            return false;
        }
        
        var registry = Constants.Constants.DefaultBuildRegistry;
        var port = Constants.Constants.DefaultBuildPort;
        var repository = Constants.Constants.DefaultBuildRepository;
        var tag = targetFramework.Replace("net", "");
        var family = Constants.Constants.DefaultBuildFamily;
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRegistry, out var registryValue))
        {
            registry = registryValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildPort, out var portValue))
        {
            port = portValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRepository, out var repositoryValue))
        {
            repository = repositoryValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildImageTag, out var tagValue))
        {
            tag = tagValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildFamily, out var familyValue))
        {
            family = familyValue;
        }

        if (string.IsNullOrEmpty(registry) || string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(tag))
        {
            return false;
        }

        image = registry;
        if (!string.IsNullOrEmpty(port))
        {
            image += $":{port}";
        }
        
        image += $"/{repository}:{tag}"; 
        if (!string.IsNullOrEmpty(family))
        {
            image += $"-{family}";
        }

        return true;
    }
    
    /// <summary>
    /// Getting the base image with the convention: REGISTRY[:PORT]/REPOSITORY[:TAG[-FAMILY]]
    /// </summary>
    public static bool TryGetBaseImage(IAnalyzerResult analyzerResult, [NotNullWhen(true)] out string? image)
    {
        image = null;
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerBaseImage, out image))
        {
            return true;
        }
        
        if (!analyzerResult.Properties.TryGetValue(MSBuildProperties.GeneralProperties.TargetFramework, out var targetFramework))
        {
            return false;
        }
        
        var registry = Constants.Constants.DefaultBaseRegistry;
        var port = Constants.Constants.DefaultBasePort;
        var repository = Constants.Constants.DefaultBaseRepository;
        var tag = targetFramework.Replace("net", "");
        var family = Constants.Constants.DefaultBaseFamily;
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRegistry, out var registryValue))
        {
            registry = registryValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerPort, out var portValue))
        {
            port = portValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerRepository, out var repositoryValue))
        {
            repository = repositoryValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerImageTag, out var tagValue))
        {
            tag = tagValue;
        }
        
        if (analyzerResult.Properties.TryGetValue(MSBuildProperties.ContainerProperties.ContainerFamily, out var familyValue))
        {
            family = familyValue;
        }

        image = registry;
        if (!string.IsNullOrEmpty(port))
        {
            image += $":{port}";
        }
        
        image += $"/{repository}:{tag}"; 
        if (!string.IsNullOrEmpty(family))
        {
            image += $"-{family}";
        }

        return true;
    }
}

// extractor classes using IMediator and CQRS pattern to handle extracting for each property and item
public sealed record BuildRegistryExtractRequest(IAnalyzerResult AnalyzerResult) : IExtractRequest<string>
{
    public sealed class BuildRegistryExtractRequestHandler : IExtractRequestHandler<BuildRegistryExtractRequest, ExtractResult<string>>
    {
        public ValueTask<ExtractResult<string>> Handle(BuildRegistryExtractRequest request, CancellationToken cancellationToken)
        {
            if (request.AnalyzerResult.Properties.TryGetValue(CustomContainerProperties.ContainerBuildRegistry, out var registry))
            {
                return Task.FromResult(registry);
            }

            return Task.FromResult(ExtractResult<string?>.Failure());
        }
    }
}

// marker interface
public interface IExtractRequest { }

public interface IExtractRequest<T> : IExtractRequest
{
}

public interface IExtractRequestHandler<TRequest, TResponse>
    where TRequest : IExtractRequest<TResponse>
{
    ValueTask<ExtractResult<TResponse>> Handle(TRequest request, CancellationToken? cancellationToken = default);
}




public sealed class ExtractResult<TValue>
{
    private readonly TValue? _value;
    public bool IsExtracted { get; }

    private ExtractResult(TValue? value, bool isExtracted)
    {
        this._value = value;
        this.IsExtracted = isExtracted;
    }
    
    public TValue Value => this.IsExtracted ? this._value! : throw new InvalidOperationException("Cannot access value when nothing was extracted");
    
    public static ExtractResult<TValue?> Success(TValue? value) => new(value, true);
    public static ExtractResult<TValue?> Failure() => new(default, false);
    
    public static implicit operator ExtractResult<TValue?>(TValue? extractedValue) => ExtractResult<TValue?>.Success(extractedValue);
}