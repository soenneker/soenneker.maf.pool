using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Maf.Cache;
using Soenneker.Maf.Cache.Abstract;
using Soenneker.Maf.Pool.Abstract;

namespace Soenneker.Maf.Pool.Registrars;

/// <summary>
/// Registration extensions for <see cref="IMafCache"/> and <see cref="IMafPool"/>.
/// </summary>
public static class MafPoolRegistrar
{
    /// <summary>
    /// Adds <see cref="IMafCache"/> and <see cref="IMafPool"/> as singleton services.
    /// </summary>
    public static IServiceCollection AddMafPoolAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IMafCache, MafCache>();
        services.TryAddSingleton<IMafPool, MafPool>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMafCache"/> and <see cref="IMafPool"/> as scoped services.
    /// </summary>
    public static IServiceCollection AddMafPoolAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IMafCache, MafCache>();
        services.TryAddScoped<IMafPool, MafPool>();

        return services;
    }
}
