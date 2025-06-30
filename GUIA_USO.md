# 🚀 Guía de Uso - Librería de Repositorio

## 📋 Resumen de Ventajas

- ✅ **Menos código:** No necesitas crear interfaces e implementaciones para cada entidad
- ✅ **Caché automático:** Mejora rendimiento sin código extra
- ✅ **Validación robusta:** Con FluentValidation
- ✅ **Mapeo automático:** Con AutoMapper
- ✅ **Paginación lista:** Sin implementar nada
- ✅ **Especificaciones:** Filtros reutilizables
- ✅ **Bulk operations:** Operaciones masivas
- ✅ **Soft delete:** Eliminación lógica

---

## 🛠️ Configuración Inicial

### 1. Crear proyecto API

```bash
dotnet new webapi -n MiApiBarrido
cd MiApiBarrido
```

### 2. Agregar referencia a la librería

```bash
dotnet add reference ../Repo/Repo.csproj
```

### 3. Instalar dependencias

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation
```

### 4. Configurar Program.cs

```csharp
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.Services;
using Repo.Repository.UnitOfWork;
using Repo.Repository.Models;

var builder = WebApplication.CreateBuilder(args);

// Entity Framework
builder.Services.AddDbContext<MiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Repositorios y servicios de la librería
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<MiDbContext>>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<MappingService>();

// Tus servicios
builder.Services.AddScoped<BarridoPeriodoService>();

var app = builder.Build();
app.Run();
```

### 5. Configurar appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MiBaseDatos;Trusted_Connection=true;TrustServerCertificate=true;",
    "Redis": "localhost:6379"
  }
}
```

---

## 📝 Ejemplo Completo

### 1. Entidad

```csharp
public class BarridoPeriodo : ISoftDelete
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 2. DTOs

```csharp
public class BarridoPeriodoListDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class BarridoPeriodoAddDTO
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}
```

### 3. Validador

```csharp
public class BarridoPeriodoValidator : AbstractValidator<BarridoPeriodo>
{
    public BarridoPeriodoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FechaInicio).NotEmpty();
        RuleFor(x => x.FechaFin).NotEmpty().GreaterThan(x => x.FechaInicio);
    }
}
```

### 4. Mapeo (AutoMapper)

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BarridoPeriodo, BarridoPeriodoListDTO>();
        CreateMap<BarridoPeriodoAddDTO, BarridoPeriodo>();
    }
}
```

### 5. Servicio

```csharp
public class BarridoPeriodoService
{
    private readonly IRepo<BarridoPeriodo> _repo;
    private readonly MappingService _mappingService;
    private readonly ValidationService _validationService;

    public BarridoPeriodoService(
        IRepo<BarridoPeriodo> repo,
        MappingService mappingService,
        ValidationService validationService)
    {
        _repo = repo;
        _mappingService = mappingService;
        _validationService = validationService;
    }

    // Obtener por ID con caché
    public async Task<BarridoPeriodoListDTO> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdWithCacheAsync(id, TimeSpan.FromMinutes(30));
        return _mappingService.Map<BarridoPeriodo, BarridoPeriodoListDTO>(entity);
    }

    // Obtener todos con caché
    public async Task<IEnumerable<BarridoPeriodoListDTO>> GetAllAsync()
    {
        var entities = await _repo.GetAllWithCacheAsync(TimeSpan.FromHours(1));
        return _mappingService.MapCollection<BarridoPeriodo, BarridoPeriodoListDTO>(entities);
    }

    // Paginación
    public async Task<PagedResult<BarridoPeriodoListDTO>> GetPagedAsync(PagedRequest request)
    {
        var result = await _repo.GetPagedAsync(request);
        var dtos = _mappingService.MapCollection<BarridoPeriodo, BarridoPeriodoListDTO>(result.Items);

        return new PagedResult<BarridoPeriodoListDTO>(dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    // Crear con validación
    public async Task<BarridoPeriodoListDTO> CreateAsync(BarridoPeriodoAddDTO dto)
    {
        var entity = _mappingService.Map<BarridoPeriodoAddDTO, BarridoPeriodo>(dto);

        // Validar
        var validator = new BarridoPeriodoValidator();
        if (!await _validationService.IsValidAsync(entity, validator))
        {
            var result = await _validationService.ValidateAsync(entity, validator);
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
        }

        // Guardar
        var created = await _repo.Insert(entity);

        // Invalidar caché
        await _repo.InvalidateCacheAsync("*");

        return _mappingService.Map<BarridoPeriodo, BarridoPeriodoListDTO>(created);
    }

    // Actualizar
    public async Task<BarridoPeriodoListDTO> UpdateAsync(int id, BarridoPeriodoAddDTO dto)
    {
        var entity = await _repo.GetById(id);
        _mappingService.Map(dto, entity);

        var updated = await _repo.UpdateAsync(entity);
        await _repo.InvalidateCacheAsync("*");

        return _mappingService.Map<BarridoPeriodo, BarridoPeriodoListDTO>(updated);
    }

    // Soft Delete
    public async Task<int> DeleteAsync(int id, string deletedBy = "admin")
    {
        var result = await _repo.SoftDeleteAsync(id, deletedBy);
        await _repo.InvalidateCacheAsync("*");
        return result;
    }

    // Restaurar
    public async Task<int> RestoreAsync(int id)
    {
        var result = await _repo.RestoreAsync(id);
        await _repo.InvalidateCacheAsync("*");
        return result;
    }

    // Bulk operations
    public async Task<int> CreateMultipleAsync(IEnumerable<BarridoPeriodoAddDTO> dtos)
    {
        var entities = _mappingService.MapCollection<BarridoPeriodoAddDTO, BarridoPeriodo>(dtos);
        var result = await _repo.AddRangeAsync(entities);
        await _repo.InvalidateCacheAsync("*");
        return result;
    }

    // Búsqueda avanzada
    public async Task<IEnumerable<BarridoPeriodoListDTO>> SearchAsync(string searchTerm)
    {
        var entities = await _repo.FindAsync(x => x.Nombre.Contains(searchTerm));
        return _mappingService.MapCollection<BarridoPeriodo, BarridoPeriodoListDTO>(entities);
    }
}
```

### 6. Controlador

```csharp
[ApiController]
[Route("api/[controller]")]
public class BarridoPeriodoController : ControllerBase
{
    private readonly BarridoPeriodoService _service;

    public BarridoPeriodoController(BarridoPeriodoService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BarridoPeriodoListDTO>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BarridoPeriodoListDTO>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<BarridoPeriodoListDTO>>> GetPaged(
        [FromQuery] PagedRequest request)
    {
        var result = await _service.GetPagedAsync(request);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BarridoPeriodoListDTO>> Create(BarridoPeriodoAddDTO dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BarridoPeriodoListDTO>> Update(int id, BarridoPeriodoAddDTO dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> Restore(int id)
    {
        await _service.RestoreAsync(id);
        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<ActionResult> CreateMultiple(IEnumerable<BarridoPeriodoAddDTO> dtos)
    {
        var result = await _service.CreateMultipleAsync(dtos);
        return Ok(new { CreatedCount = result });
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BarridoPeriodoListDTO>>> Search([FromQuery] string term)
    {
        var result = await _service.SearchAsync(term);
        return Ok(result);
    }
}
```

---

## 🎯 Ejemplos de Uso

### Paginación

```http
GET /api/barridoperiodo/paged?pageNumber=1&pageSize=10&sortBy=Nombre&isAscending=true
```

### Búsqueda

```http
GET /api/barridoperiodo/search?term=enero
```

### Crear múltiples

```http
POST /api/barridoperiodo/bulk
Content-Type: application/json

[
  {
    "nombre": "Enero 2024",
    "fechaInicio": "2024-01-01",
    "fechaFin": "2024-01-31"
  },
  {
    "nombre": "Febrero 2024",
    "fechaInicio": "2024-02-01",
    "fechaFin": "2024-02-29"
  }
]
```

---

## 🔧 Características Disponibles

### ✅ **CRUD Básico**

- `GetById()` - Obtener por ID
- `GetAll()` - Obtener todos
- `Insert()` - Crear
- `Update()` - Actualizar
- `Delete()` - Eliminar

### ✅ **Caché Automático**

- `GetByIdWithCacheAsync()` - Con caché
- `GetAllWithCacheAsync()` - Con caché
- `InvalidateCacheAsync()` - Invalidar caché

### ✅ **Paginación**

- `GetPagedAsync()` - Paginación automática
- `PagedRequest` - Configuración de paginación

### ✅ **Validación**

- `ValidationService` - Servicio de validación
- `FluentValidation` - Validadores

### ✅ **Mapeo**

- `MappingService` - Servicio de mapeo
- `AutoMapper` - Mapeo automático

### ✅ **Bulk Operations**

- `AddRangeAsync()` - Crear múltiples
- `UpdateRangeAsync()` - Actualizar múltiples
- `DeleteRangeAsync()` - Eliminar múltiples

### ✅ **Soft Delete**

- `SoftDeleteAsync()` - Eliminación lógica
- `RestoreAsync()` - Restaurar
- `GetAllIncludingDeletedAsync()` - Incluir eliminados

### ✅ **Búsqueda Avanzada**

- `FindAsync()` - Búsqueda con filtros
- `FirstOrDefaultAsync()` - Primer elemento
- `AnyAsync()` - Verificar existencia
- `CountAsync()` - Contar registros

---

## 🚀 ¡Listo para usar!

Con esta configuración tienes:

- ✅ Repositorio genérico (no necesitas crear uno por entidad)
- ✅ Caché automático con Redis
- ✅ Validación robusta
- ✅ Mapeo automático
- ✅ Paginación lista
- ✅ Operaciones masivas
- ✅ Soft delete
- ✅ Búsqueda avanzada

¡Solo necesitas crear tus entidades, DTOs, validadores y mapeos!
