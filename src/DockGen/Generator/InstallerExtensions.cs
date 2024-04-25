using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DockGen.Generator;

public static class InstallerExtensions
{
    public static void AddGeneratorCore(this IServiceCollection services)
    {
        services.AddSingleton<DockerfileGenerator>();
        services.AddTransient<IExtractor, Extractor>();
        services.AddExtractorsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    public static void AddExtractorsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var extractors = assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IExtractRequestHandler<,>)));
        foreach (var extractor in extractors)
        {
            var interfaces = extractor.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IExtractRequestHandler<,>));
            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, extractor);
            }
        }
    }
}