# Repo.Integrations.Mapperly

Integración opcional con [Mapperly](https://mapperly.riok.app/) para **AdvancedRepository.NET8**. Esta integración permite mapear entidades a DTOs en tiempo de compilación sin agregar dependencias al paquete core.

## ¿Por qué Mapperly?

- **Zero runtime overhead**: El mapeo se genera en tiempo de compilación
- **Sin reflection**: Rendimiento máximo en escenarios de alta carga
- **AOT-friendly**: Compatible con Native AOT y trimming
- **Descubrimiento en tiempo de diseño**: Errores de mapeo se detectan al compilar

## Instalación

```bash
dotnet add package AdvancedRepository.Integrations.Mapperly
```

## Uso Básico

### 1. Define tus DTOs

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

### 2. Crea tu Mapper con Mapperly

```csharp
using Riok.Mapperly.Abstractions;
using YourNamespace.Entities;
using YourNamespace.DTOs;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User entity);
    public partial User ToEntity(CreateUserRequest request);
    public partial IEnumerable<UserDto> ToDtoCollection(IEnumerable<User> entities);
    
    // Mapperly genera automáticamente el código de mapeo
}
```

### 3. Úsalo en tu API/Controlador

```csharp
public class UsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;
    private readonly UserMapper _mapper = new();

    public UsersController(IRepo<User> userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userRepo.GetAllAsync();
        return Ok(_mapper.ToDtoCollection(users));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _userRepo.GetById(id);
        if (user == null) return NotFound();
        return Ok(_mapper.ToDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var entity = _mapper.ToEntity(request);
        entity.CreatedAt = DateTime.UtcNow;
        
        var created = await _userRepo.Insert(entity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.ToDto(created));
    }
}
```

## Configuración Avanzada

### Mapeo con transformaciones

```csharp
[Mapper]
public partial class OrderMapper
{
    [MapProperty(nameof(Order.TotalAmount), nameof(OrderDto.FormattedTotal))]
    public partial OrderDto ToDto(Order order);

    // Método de conversión personalizado
    private string FormatCurrency(decimal amount) => $"${amount:N2}";
}
```

### Ignorar propiedades

```csharp
[Mapper]
public partial class ProductMapper
{
    [MapperIgnoreTarget(nameof(ProductDto.InternalCode))]
    public partial ProductDto ToDto(Product entity);
}
```

### Mapeo bidireccional

```csharp
[Mapper]
public partial class CategoryMapper
{
    public partial CategoryDto ToDto(Category entity);
    public partial Category ToEntity(CategoryDto dto);
}
```

## Patrones Recomendados

### 1. Mapper como Singleton

Los mappers de Mapperly son stateless y thread-safe:

```csharp
// En Program.cs
builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<ProductMapper>();
```

### 2. Extensión del Repositorio (opcional)

Si necesitas mapeo directo en el repositorio, extiende `RepoBase`:

```csharp
public class UserRepository : RepoBase<User, YourDbContext>
{
    private readonly UserMapper _mapper;

    public UserRepository(
        YourDbContext context, 
        ILogger logger, 
        UserMapper mapper) : base(context, logger)
    {
        _mapper = mapper;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsDtoAsync()
    {
        var entities = await GetAllAsync();
        return _mapper.ToDtoCollection(entities);
    }
}
```

### 3. Capa de Servicios con Mapeo

```csharp
public class UserService
{
    private readonly IRepo<User> _userRepo;
    private readonly UserMapper _mapper;

    public UserService(IRepo<User> userRepo, UserMapper mapper)
    {
        _userRepo = userRepo;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetUserAsync(int id)
    {
        var user = await _userRepo.GetById(id);
        return user != null ? _mapper.ToDto(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
    {
        var spec = new ActiveUsersSpecification();
        var users = await _userRepo.GetAllBySpecAsync(spec);
        return _mapper.ToDtoCollection(users);
    }
}
```

## Arquitectura: ¿Dónde va el mapeo?

```
┌─────────────────────────────────────────────────────────────┐
│  Controller/API Layer                                       │
│  - Recibe DTOs/Requests                                     │
│  - Devuelve DTOs/Responses                                  │
│  - Usa Mapperly para convertir a/desde entidades            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Service Layer (opcional)                                   │
│  - Lógica de negocio                                        │
│  - Orquesta operaciones                                     │
│  - Puede usar mappers si transforma datos                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Repository Layer (Core)                                    │
│  - Trabaja SOLO con Entidades                               │
│  - NO sabe nada de DTOs ni mappers                          │
│  - IRepo<T> solo acepta y devuelve T (entidad)              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Database                                                   │
│  - Persiste entidades                                       │
└─────────────────────────────────────────────────────────────┘
```

**Regla de oro**: El repositorio **nunca** devuelve DTOs. El mapeo ocurre en la capa de aplicación/controladores.

## Comparación: Mapperly vs AutoMapper

| Característica | Mapperly | AutoMapper |
|---------------|----------|------------|
| Tiempo de generación | Compilación | Runtime |
| Reflection | No | Sí |
| Performance | Máxima | Buena |
| Configuración | Atributos/Convenio | Fluent API |
| Debugging | Código generado visible | Perfil interno |
| AOT Support | ✅ Completo | ⚠️ Limitado |

## Requisitos

- .NET 8.0 o .NET 9.0
- Riok.Mapperly 4.1.0+
- AdvancedRepository.NET8 1.0.0+

## Licencia

MIT - Ver LICENSE para más detalles.
