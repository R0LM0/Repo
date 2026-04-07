using Microsoft.Extensions.DependencyInjection;

namespace Repo.Integrations.Mapperly;

/// <summary>
/// Extensiones de IServiceCollection para registrar mappers de Mapperly.
/// </summary>
public static class MapperlyServiceExtensions
{
    /// <summary>
    /// Registra un mapper de Mapperly como Singleton.
    /// </summary>
    /// <typeparam name="TMapper">El tipo del mapper a registrar.</typeparam>
    /// <param name="services">La colección de servicios.</param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    /// <remarks>
    /// Los mappers de Mapperly son stateless y thread-safe, por lo que
    /// pueden registrarse como Singleton para máximo rendimiento.
    /// </remarks>
    public static IServiceCollection AddRepoMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, new()
    {
        services.AddSingleton<TMapper>(new TMapper());
        return services;
    }
    
    /// <summary>
    /// Registra múltiples mappers de Mapperly.
    /// </summary>
    /// <param name="services">La colección de servicios.</param>
    /// <param name="mapperTypes">Los tipos de los mappers a registrar.</param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddRepoMappers(this IServiceCollection services, params Type[] mapperTypes)
    {
        foreach (var mapperType in mapperTypes)
        {
            // Verificar que tiene el atributo [Mapper]
            var hasMapperAttribute = mapperType.GetCustomAttributes(typeof(Riok.Mapperly.Abstractions.MapperAttribute), false).Any();
            if (!hasMapperAttribute)
            {
                throw new InvalidOperationException(
                    $"El tipo {mapperType.Name} no tiene el atributo [Mapper]. " +
                    "Asegúrate de decorar tu clase con [Mapper] de Riok.Mapperly.");
            }
            
            services.AddSingleton(mapperType, Activator.CreateInstance(mapperType)!);
        }
        
        return services;
    }
}
