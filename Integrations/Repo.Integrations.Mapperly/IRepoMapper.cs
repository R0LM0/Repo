using Riok.Mapperly.Abstractions;

namespace Repo.Integrations.Mapperly;

/// <summary>
/// Interfaz base para todos los mappers de Mapperly en el ecosistema Repo.
/// </summary>
/// <remarks>
/// Los mappers generados por Mapperly son:
/// - Stateless (sin estado interno)
/// - Thread-safe (seguros para uso concurrente)
/// - Zero-allocation (cuando es posible)
/// 
/// Por lo tanto, pueden registrarse como Singleton en DI.
/// </remarks>
public interface IRepoMapper<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    /// <summary>
    /// Mapea una entidad a su DTO correspondiente.
    /// </summary>
    TDto ToDto(TEntity entity);
    
    /// <summary>
    /// Mapea un DTO a su entidad correspondiente.
    /// </summary>
    TEntity ToEntity(TDto dto);
    
    /// <summary>
    /// Mapea una colección de entidades a DTOs.
    /// </summary>
    IEnumerable<TDto> ToDtoCollection(IEnumerable<TEntity> entities);
    
    /// <summary>
    /// Mapea una colección de DTOs a entidades.
    /// </summary>
    IEnumerable<TEntity> ToEntityCollection(IEnumerable<TDto> dtos);
}

/// <summary>
/// Interfaz para mappers que soportan proyección en consultas IQueryable.
/// </summary>
/// <remarks>
/// Esta interfaz permite usar Mapperly con proyecciones de EF Core
/// para consultas optimizadas que solo seleccionan los campos necesarios.
/// </remarks>
public interface IProjectionMapper<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    /// <summary>
    /// Obtiene una expresión de proyección para usar en consultas IQueryable.
    /// </summary>
    /// <example>
    /// var query = await _userRepo.AsQueryable()
    ///     .Select(_mapper.ProjectToDto())
    ///     .ToListAsync();
    /// </example>
    System.Linq.Expressions.Expression<Func<TEntity, TDto>> ProjectToDto();
}
