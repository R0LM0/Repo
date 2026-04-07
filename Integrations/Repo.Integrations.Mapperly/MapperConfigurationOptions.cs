namespace Repo.Integrations.Mapperly;

/// <summary>
/// Opciones de configuración para los mappers de Repo.
/// </summary>
public class MapperConfigurationOptions
{
    /// <summary>
    /// Indica si se debe ignorar valores nulos durante el mapeo.
    /// Por defecto: true
    /// </summary>
    public bool IgnoreNullValues { get; set; } = true;
    
    /// <summary>
    /// Indica si se debe usar enum como string en el mapeo.
    /// Por defecto: false
    /// </summary>
    public bool EnumMappingAsString { get; set; } = false;
    
    /// <summary>
    /// Formato por defecto para fechas.
    /// Por defecto: "yyyy-MM-ddTHH:mm:ssZ" (ISO 8601)
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ssZ";
}

/// <summary>
/// Atributo para marcar DTOs que son usados en operaciones de creación.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CreateRequestAttribute : Attribute { }

/// <summary>
/// Atributo para marcar DTOs que son usados en operaciones de actualización.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UpdateRequestAttribute : Attribute { }

/// <summary>
/// Atributo para marcar DTOs que son usados como respuestas de API.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ResponseDtoAttribute : Attribute { }
