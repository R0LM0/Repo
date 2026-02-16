# 🚀 Librería de Repositorio Avanzada para .NET 8

Una librería completa y optimizada para el patrón Repository con funcionalidades avanzadas para .NET 8.

## 🗄️ **Soporte Multi-Proveedor**

✅ **SQL Server** - Soporte completo  
✅ **PostgreSQL** - Soporte completo  
✅ **Configuración dinámica** - Cambia de proveedor sin cambiar código

## ✨ Características Principales

### 🔧 **Funcionalidades Básicas**

- ✅ Operaciones CRUD completas (Create, Read, Update, Delete)
- ✅ Soporte para operaciones síncronas y asíncronas
- ✅ Transacciones de base de datos
- ✅ Logging integrado
- ✅ Procedimientos almacenados

### 🚀 **Funcionalidades Avanzadas**

- 📄 **Paginación y Filtrado**: Paginación automática con filtros dinámicos
- 🔍 **Especificaciones (Specification Pattern)**: Filtros reutilizables y composables
- 💾 **Caché con Redis**: Caché distribuido para mejorar el rendimiento
- ✅ **Validación con FluentValidation**: Validación robusta de entidades
- 🔄 **AutoMapper**: Mapeo automático entre entidades y DTOs
- 🗑️ **Soft Delete**: Eliminación lógica con capacidad de restauración
- 📦 **Bulk Operations**: Operaciones masivas para mejor rendimiento
- 🏗️ **Unit of Work**: Gestión de transacciones y contexto
- 🔍 **Búsqueda Avanzada**: Filtros dinámicos y búsqueda por texto

## 📦 Instalación

### Instalación Básica

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.Extensions.Logging.Abstractions
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package AutoMapper
dotnet add package FluentValidation
```

### Instalación del Proveedor de Base de Datos

**Para SQL Server:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

**Para PostgreSQL:**
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**Nota:** La librería soporta ambos proveedores. Puedes usar SQL Server, PostgreSQL, o ambos según tus necesidades.

## 🏗️ Estructura del Proyecto

```
Repository/
├── Base/
│   ├── IRepo.cs                    # Interfaz principal del repositorio
│   ├── RepoBase.cs                 # Implementación base
│   └── RepoBaseEnhanced.cs         # Versión mejorada con funcionalidades avanzadas
├── Models/
│   └── PagedResult.cs              # Modelos para paginación
├── Specifications/
│   ├── ISpecification.cs           # Interfaz para especificaciones
│   ├── BaseSpecification.cs        # Clase base para especificaciones
│   ├── SpecificationEvaluator.cs   # Evaluador de especificaciones
│   └── Examples/
│       └── UserSpecifications.cs   # Ejemplos de especificaciones
├── Interfaces/
│   ├── IAuditableEntity.cs         # Interfaz para entidades auditables
│   └── ICacheService.cs            # Interfaz para servicio de caché
├── Services/
│   ├── RedisCacheService.cs        # Implementación de caché con Redis
│   ├── ValidationService.cs        # Servicio de validación
│   └── MappingService.cs           # Servicio de mapeo
└── UnitOfWork/
    ├── IUnitOfWork.cs              # Interfaz Unit of Work
    └── UnitOfWork.cs               # Implementación Unit of Work
```

## 🚀 Uso Básico

### 1. Configuración Básica

#### Opción A: Configuración Manual (SQL Server o PostgreSQL)

**Para SQL Server:**
```csharp
// En Program.cs o Startup.cs
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
```

**Para PostgreSQL:**
```csharp
// En Program.cs o Startup.cs
services.AddDbContext<YourDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
```

#### Opción B: Configuración Dinámica Multi-Proveedor (Recomendado)

```csharp
// En Program.cs o Startup.cs
using Repo.Repository.Extensions;

// Configuración automática según appsettings.json
services.AddDbContextWithProvider<YourDbContext>(builder.Configuration);
services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
```

**Configuración en appsettings.json:**
```json
{
  "Database": {
    "Provider": "PostgreSQL",  // o "SqlServer"
    "PostgreSQL": {
      "ConnectionString": "Host=localhost;Database=mydb;Username=postgres;Password=password"
    },
    "SqlServer": {
      "ConnectionString": "Server=localhost;Database=mydb;User Id=sa;Password=password;TrustServerCertificate=true"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydb;Username=postgres;Password=password"
  }
}
```

### 2. Uso del Repositorio

```csharp
public class UserService
{
    private readonly IRepo<User> _userRepo;

    public UserService(IRepo<User> userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        return await _userRepo.Insert(user);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepo.GetAllAsync();
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _userRepo.GetById(id);
    }
}
```

## 🔍 Paginación y Filtrado

```csharp
// Crear request de paginación
var request = new PagedRequest
{
    PageNumber = 1,
    PageSize = 10,
    SortBy = "Name",
    IsAscending = true,
    SearchTerm = "john",
    Filters = new Dictionary<string, object>
    {
        { "IsActive", true },
        { "Role", "Admin" }
    }
};

// Obtener resultados paginados
var result = await _userRepo.GetPagedAsync(request);

Console.WriteLine($"Total: {result.TotalCount}");
Console.WriteLine($"Página {result.PageNumber} de {result.TotalPages}");
foreach (var user in result.Items)
{
    Console.WriteLine(user.Name);
}
```

## 🎯 Especificaciones (Specification Pattern)

```csharp
// Crear especificación
public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
    {
        AddCriteria(user => user.IsActive);
        AddInclude(user => user.Profile);
        AddOrderBy(user => user.CreatedAt);
    }
}

// Usar especificación
var spec = new ActiveUsersSpecification();
var activeUsers = await _userRepo.GetAllBySpecAsync(spec);

// Especificación con paginación
var pagedSpec = new UsersWithPaginationSpecification(1, 10, "Name", true);
var pagedUsers = await _userRepo.GetPagedBySpecAsync(pagedSpec, request);
```

## 💾 Caché con Redis

```csharp
// Configurar Redis
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
services.AddScoped<ICacheService, RedisCacheService>();

// Usar caché
var user = await _userRepo.GetByIdWithCacheAsync(1, TimeSpan.FromMinutes(30));
var allUsers = await _userRepo.GetAllWithCacheAsync(TimeSpan.FromHours(1));

// Invalidar caché
await _userRepo.InvalidateCacheAsync("*");
```

## ✅ Validación con FluentValidation

```csharp
// Crear validador
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(18, 100);
    }
}

// Usar validación
var validator = new UserValidator();
var validationService = new ValidationService(logger);

var isValid = await validationService.IsValidAsync(user, validator);
if (!isValid)
{
    var result = await validationService.ValidateAsync(user, validator);
    // Manejar errores de validación
}
```

## 🔄 AutoMapper

```csharp
// Configurar AutoMapper
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<User, UserDto>();
    cfg.CreateMap<UserDto, User>();
});

var mapper = config.CreateMapper();
var mappingService = new MappingService(mapper, logger);

// Usar mapeo
var userDto = mappingService.Map<User, UserDto>(user);
var usersDto = mappingService.MapCollection<User, UserDto>(users);
```

## 🗑️ Soft Delete

```csharp
// Implementar ISoftDelete en tu entidad
public class User : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Usar soft delete
await _userRepo.SoftDeleteAsync(1, "admin");
await _userRepo.RestoreAsync(1);

// Obtener incluyendo eliminados
var allUsers = await _userRepo.GetAllIncludingDeletedAsync();
```

## 📦 Bulk Operations

```csharp
// Operaciones masivas
var users = new List<User> { /* ... */ };

// Agregar múltiples usuarios
await _userRepo.AddRangeAsync(users);

// Actualizar múltiples usuarios
await _userRepo.UpdateRangeAsync(users);

// Eliminar múltiples usuarios
await _userRepo.DeleteRangeAsync(users);

// Eliminar por condición
await _userRepo.DeleteRangeAsync(user => user.IsActive == false);
```

## 🏗️ Unit of Work

```csharp
public class UserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateUserWithProfileAsync(User user, UserProfile profile)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var userRepo = _unitOfWork.Repository<User>();
            var profileRepo = _unitOfWork.Repository<UserProfile>();

            await userRepo.Insert(user);
            await profileRepo.Insert(profile);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

## 🔍 Búsqueda Avanzada

```csharp
// Búsqueda con filtros
var users = await _userRepo.FindAsync(user => user.IsActive && user.Age > 18);

// Búsqueda con includes
var usersWithProfile = await _userRepo.FindAsync(
    user => user.IsActive,
    user => user.Profile,
    user => user.Roles
);

// Verificar existencia
var exists = await _userRepo.AnyAsync(user => user.Email == "test@example.com");

// Contar registros
var count = await _userRepo.CountAsync(user => user.IsActive);
```

## 🚀 Configuración Avanzada

### Configuración Completa en Program.cs

```csharp
using Repo.Repository.Extensions;
using Repo.Repository.Services;

var builder = WebApplication.CreateBuilder(args);

// Entity Framework con soporte multi-proveedor
builder.Services.AddDbContextWithProvider<YourDbContext>(builder.Configuration);

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Repositorios y servicios
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<DatabaseProviderService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<MappingService>();

// Logging
builder.Services.AddLogging();
```

## 🗄️ Soporte Multi-Proveedor (SQL Server y PostgreSQL)

### Características

✅ **Soporte completo para SQL Server y PostgreSQL**
✅ **Configuración dinámica mediante appsettings.json**
✅ **Código agnóstico al proveedor de base de datos**
✅ **Migraciones independientes por proveedor**
✅ **Mismo código, múltiples bases de datos**

### Diferencias Importantes

#### Procedimientos Almacenados

**SQL Server:**
```csharp
await _repo.ExecuteStoredProcedureNonQueryAsync("EXEC sp_GetUsers @UserId", parameter);
```

**PostgreSQL:**
```csharp
await _repo.ExecuteStoredProcedureNonQueryAsync("CALL sp_get_users(@UserId)", parameter);
```

#### Tipos de Datos

- **DateTime**: Ambos proveedores soportan `DateTime` y `DateTimeOffset`
- **Strings**: SQL Server usa `nvarchar(max)`, PostgreSQL usa `text`
- **Identity/Sequences**: SQL Server usa `IDENTITY`, PostgreSQL usa `SERIAL` o `SEQUENCES`

### Ejemplo de Configuración por Ambiente

**appsettings.Development.json (PostgreSQL):**
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "PostgreSQL": {
      "ConnectionString": "Host=localhost;Database=portfolio_dev;Username=postgres;Password=dev123"
    }
  }
}
```

**appsettings.Production.json (SQL Server):**
```json
{
  "Database": {
    "Provider": "SqlServer",
    "SqlServer": {
      "ConnectionString": "Server=prod-server;Database=portfolio;User Id=appuser;Password=***;TrustServerCertificate=true"
    }
  }
}
```

### Migraciones

**Para SQL Server:**
```bash
dotnet ef migrations add InitialCreate --context YourDbContext
dotnet ef database update --context YourDbContext
```

**Para PostgreSQL:**
```bash
dotnet ef migrations add InitialCreate --context YourDbContext
dotnet ef database update --context YourDbContext
```

**Nota:** Las migraciones se generan automáticamente según el proveedor configurado en `appsettings.json`.

## 📊 Rendimiento y Optimización

### 1. **Caché Inteligente**

- Caché automático para consultas frecuentes
- Invalidación automática al modificar datos
- Configuración de TTL personalizable

### 2. **Bulk Operations**

- Operaciones masivas para mejor rendimiento
- Reducción de llamadas a la base de datos
- Transacciones optimizadas

### 3. **Especificaciones**

- Filtros reutilizables
- Composición de criterios
- Optimización de consultas

### 4. **Paginación Eficiente**

- Paginación a nivel de base de datos
- Filtros dinámicos
- Ordenamiento optimizado

## 🔧 Personalización

### Crear Especificaciones Personalizadas

```csharp
public class UsersByDateRangeSpecification : BaseSpecification<User>
{
    public UsersByDateRangeSpecification(DateTime startDate, DateTime endDate)
    {
        AddCriteria(user => user.CreatedAt >= startDate && user.CreatedAt <= endDate);
        AddOrderByDescending(user => user.CreatedAt);
    }
}
```

### Extender el Repositorio Base

```csharp
public class UserRepository : RepoBaseEnhanced<User, YourDbContext>
{
    public UserRepository(YourDbContext context, ILogger logger, ICacheService cacheService)
        : base(context, logger, cacheService)
    {
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
    {
        return await FindAsync(user => user.Role == role);
    }
}
```

## 🧪 Testing

```csharp
[Test]
public async Task GetPagedAsync_ShouldReturnPagedResults()
{
    // Arrange
    var request = new PagedRequest { PageNumber = 1, PageSize = 10 };

    // Act
    var result = await _userRepo.GetPagedAsync(request);

    // Assert
    Assert.That(result.Items, Is.Not.Null);
    Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(0));
    Assert.That(result.PageNumber, Is.EqualTo(1));
}
```

## 📝 Notas Importantes sobre Multi-Proveedor

### Compatibilidad de Código

✅ **Totalmente compatible**: La mayoría del código funciona igual en ambos proveedores
⚠️ **Procedimientos almacenados**: Requieren sintaxis específica del proveedor
⚠️ **Funciones SQL**: Algunas funciones difieren (GETDATE() vs NOW(), etc.)
⚠️ **Tipos de datos**: Algunos tipos tienen diferencias menores

### Mejores Prácticas

1. **Usa EF Core siempre que sea posible**: Evita SQL crudo para máxima portabilidad
2. **Configuración por ambiente**: Usa diferentes proveedores en dev/prod según necesidades
3. **Migraciones separadas**: Considera crear migraciones específicas por proveedor si usas SQL específico
4. **Pruebas**: Prueba tu código con ambos proveedores si planeas soportar ambos

### Ejemplo de appsettings.json

Ver `appsettings.example.json` en la raíz del proyecto para un ejemplo completo de configuración.

## 📝 Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor, abre un issue o un pull request.

## 📞 Soporte

Si tienes alguna pregunta o necesitas ayuda, por favor abre un issue en el repositorio.

---

**¡Disfruta usando esta librería de repositorio avanzada con soporte multi-proveedor! 🚀**
