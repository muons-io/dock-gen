using System.Diagnostics.CodeAnalysis;
using Buildalyzer;
using DockGen.Constants;

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