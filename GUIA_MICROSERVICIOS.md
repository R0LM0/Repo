# 🏗️ Guía: Usar la Librería de Repositorio en Microservicios

## ✅ ¿Por qué esta librería es ideal para microservicios?

### 1. **Aislamiento de Datos**
Cada microservicio puede tener su propia base de datos:
- ✅ Soporte multi-proveedor (SQL Server, PostgreSQL)
- ✅ Configuración independiente por servicio
- ✅ Unit of Work por contexto de servicio

### 2. **Escalabilidad**
- ✅ Caché distribuido con Redis
- ✅ Operaciones asíncronas
- ✅ Bulk operations para alto rendimiento
- ✅ Paginación eficiente

### 3. **Mantenibilidad**
- ✅ Patrón Repository para abstracción
- ✅ Especificaciones reutilizables
- ✅ Validación centralizada
- ✅ Logging integrado

## 🏗️ Arquitectura Recomendada para Microservicios

### Estructura de un Microservicio

```
Microservicio.Users/
├── Controllers/
│   └── UsersController.cs
├── Services/
│   ├── UserService.cs          # Lógica de negocio
│   └── UserDomainService.cs   # Reglas de dominio
├── Repositories/               # (Opcional - si necesitas repositorios específicos)
│   └── UserRepository.cs
├── Models/
│   ├── User.cs                 # Entidad
│   ├── UserDto.cs              # DTO para API
│   └── CreateUserRequest.cs    # Request DTO
├── Data/
│   └── UsersDbContext.cs       # DbContext específico del servicio
└── Program.cs
```

## 📝 Ejemplo: Microservicio de Usuarios

### 1. Configuración del Microservicio

```csharp
// Program.cs
using Repo.Repository.Extensions;
using Repo.Repository.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar base de datos específica del microservicio
builder.Services.AddDbContextWithProvider<UsersDbContext>(
    builder.Configuration,
    connectionString: builder.Configuration.GetConnectionString("UsersDb")
);

// Configurar repositorios
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<UsersDbContext>>();

// Configurar caché distribuido (compartido entre instancias)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "UsersService"; // Prefijo único por servicio
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UsersDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### 2. DbContext Específico del Microservicio

```csharp
// Data/UsersDbContext.cs
using Microsoft.EntityFrameworkCore;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) 
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuraciones específicas del dominio de usuarios
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
        });
    }
}
```

### 3. Servicio de Aplicación

```csharp
// Services/UserService.cs
using Repo.Repository.Base;
using Repo.Repository.UnitOfWork;
using Repo.Repository.Models;

public class UserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var userRepo = _unitOfWork.Repository<User>();
            
            // Validar que el email no exista
            var exists = await userRepo.AnyAsync(u => u.Email == request.Email);
            if (exists)
                throw new InvalidOperationException("El email ya está registrado");

            var user = new User
            {
                Email = request.Email,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await userRepo.Insert(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Usuario creado: {UserId}", createdUser.Id);
            
            return MapToDto(createdUser);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize)
    {
        var userRepo = _unitOfWork.Repository<User>();
        
        var request = new PagedRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            SortBy = "CreatedAt",
            IsAscending = false
        };

        var result = await userRepo.GetPagedAsync(request);
        
        return new PagedResult<UserDto>(
            result.Items.Select(MapToDto),
            result.TotalCount,
            result.PageNumber,
            result.PageSize
        );
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var userRepo = _unitOfWork.Repository<User>();
        
        // Usar caché para mejorar rendimiento
        var user = await userRepo.GetByIdWithCacheAsync(
            id, 
            TimeSpan.FromMinutes(15)
        );

        return user != null ? MapToDto(user) : null;
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt
        };
    }
}
```

### 4. Controller

```csharp
// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetUsersAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }
}
```

## 🔧 Configuración por Ambiente

### appsettings.Development.json

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "PostgreSQL": {
      "ConnectionString": "Host=localhost;Database=users_dev;Username=postgres;Password=dev123"
    }
  },
  "ConnectionStrings": {
    "UsersDb": "Host=localhost;Database=users_dev;Username=postgres;Password=dev123",
    "Redis": "localhost:6379"
  }
}
```

### appsettings.Production.json

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "PostgreSQL": {
      "ConnectionString": "Host=users-db.production;Database=users_prod;Username=app_user;Password=${DB_PASSWORD}"
    }
  },
  "ConnectionStrings": {
    "UsersDb": "Host=users-db.production;Database=users_prod;Username=app_user;Password=${DB_PASSWORD}",
    "Redis": "redis-cluster.production:6379"
  }
}
```

## 🎯 Mejores Prácticas para Microservicios

### 1. **Base de Datos por Servicio**
✅ Cada microservicio tiene su propia base de datos
✅ No compartir tablas entre servicios
✅ Comunicación entre servicios vía API/Eventos

### 2. **Caché Distribuido**
✅ Usar Redis para caché compartido entre instancias
✅ Invalidar caché cuando se actualizan datos
✅ Configurar TTL apropiado según el caso de uso

### 3. **Transacciones**
✅ Usar Unit of Work para transacciones dentro del servicio
✅ Para transacciones distribuidas, usar patrones Saga o Event Sourcing
✅ No usar transacciones distribuidas (2PC)

### 4. **Logging y Observabilidad**
✅ Logging estructurado con contexto del servicio
✅ Correlation IDs para rastrear requests entre servicios
✅ Health checks para monitoreo

### 5. **Especificaciones Reutilizables**
```csharp
// Crear especificaciones específicas del dominio
public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
    {
        AddCriteria(user => user.IsActive && !user.IsDeleted);
        AddOrderByDescending(user => user.CreatedAt);
    }
}

// Usar en el servicio
var spec = new ActiveUsersSpecification();
var activeUsers = await userRepo.GetAllBySpecAsync(spec);
```

## 🚀 Patrones Adicionales Recomendados

### 1. **CQRS (Command Query Responsibility Segregation)**

```csharp
// Commands (Escritura)
public class CreateUserCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<UserDto> Handle(CreateUserCommand command)
    {
        // Lógica de creación
    }
}

// Queries (Lectura)
public class GetUsersQueryHandler
{
    private readonly IRepo<User> _userRepo;
    
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery query)
    {
        // Lógica de consulta
    }
}
```

### 2. **Event Sourcing** (Opcional)

Para servicios que requieren auditoría completa o reconstrucción de estado.

### 3. **API Gateway**

Usar un API Gateway para:
- Enrutamiento a microservicios
- Autenticación/Authorization centralizada
- Rate limiting
- Load balancing

## ⚠️ Consideraciones Importantes

### 1. **Consistencia de Datos**
- ✅ Consistencia eventual entre servicios
- ❌ Evitar transacciones distribuidas
- ✅ Usar eventos para sincronización

### 2. **Idempotencia**
- ✅ Hacer operaciones idempotentes
- ✅ Usar IDs únicos para evitar duplicados

### 3. **Versionado de API**
- ✅ Versionar endpoints: `/api/v1/users`
- ✅ Mantener compatibilidad hacia atrás

### 4. **Testing**
```csharp
// Usar In-Memory Database para tests
services.AddDbContext<UsersDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

## 📊 Ejemplo de Comunicación entre Microservicios

```csharp
// Servicio A llama a Servicio B vía HTTP
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // Validar usuario existe (llamada a Users Service)
        var userResponse = await _httpClient.GetAsync(
            $"http://users-service/api/users/{request.UserId}"
        );
        
        if (!userResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Usuario no encontrado");

        // Crear orden en base de datos local
        var orderRepo = _unitOfWork.Repository<Order>();
        var order = new Order { UserId = request.UserId, ... };
        await orderRepo.Insert(order);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## ✅ Conclusión

Esta librería es **perfectamente adecuada** para microservicios porque:

1. ✅ Permite aislamiento de datos por servicio
2. ✅ Soporta caché distribuido
3. ✅ Facilita testing con diferentes proveedores
4. ✅ Proporciona abstracción que facilita cambios
5. ✅ Incluye funcionalidades necesarias (paginación, caché, validación)

**Recomendación:** Úsala como base y agrega patrones específicos de microservicios según tus necesidades (CQRS, Event Sourcing, etc.).
