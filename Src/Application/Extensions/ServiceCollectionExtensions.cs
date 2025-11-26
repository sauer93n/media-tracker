using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the assembly for AutoMapper Profile classes and registers them automatically
    /// </summary>
    public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, Assembly assembly)
    {
        var profiles = assembly.GetTypes()
            .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        services.AddAutoMapper(cfg =>
        {
            foreach (var profile in profiles)
                cfg.AddProfile(profile);
        });

        return services;
    }
}