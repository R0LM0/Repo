# 🚀 Guía Completa: Usar Librería de Repositorio en API .NET 9

## 📋 Índice

1. [Instalación y Configuración](#instalación-y-configuración)
2. [Configuración Básica](#configuración-básica)
3. [Uso en Controllers](#uso-en-controllers)
4. [Configuración Avanzada](#configuración-avanzada)
5. [Ejemplos Prácticos](#ejemplos-prácticos)
6. [Mejores Prácticas](#mejores-prácticas)
7. [Troubleshooting](#troubleshooting)

## 🔧 Instalación y Configuración

### 1. Instalar la Librería

#### Opción A: Desde NuGet (recomendado)

```bash
# Instalar desde NuGet.org (cuando esté publicado)
dotnet add package AdvancedRepository.NET9

# O instalar desde fuente local
dotnet add package AdvancedRepository.NET9 --source ./nupkgs
```

#### Opción B: Referencia Directa del Proyecto

```xml
<!-- En tu .csproj -->
<ItemGroup>
  <ProjectReference Include="../Repo/Repo.csproj" />
</ItemGroup>
```

### 2. Dependencias Requeridas

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.6" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.6" />
  <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.6" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="13.0.1" />
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.9.0" />
</ItemGroup>
```

## ⚙️ Configuración Básica

### 1. Program.cs - Configuración Principal

```csharp
using Microsoft.EntityFrameworkCore;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.Services;
using Repo.Repository.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar Repositorio Base
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));

// Configurar Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();

// Configurar Servicios Adicionales
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<MappingService>();

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Configurar Redis Cache (opcional)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Configurar FluentValidation
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();
```

### 2. DbContext de Ejemplo

```csharp
using Microsoft.EntityFrameworkCore;
using Repo.Repository.Interfaces;

public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuraciones específicas de entidades
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
```

### 3. Entidades de Ejemplo

```csharp
using Repo.Repository.Interfaces;

public class User : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class Product : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

## 🎮 Uso en Controllers

### 1. Controller Básico

```csharp
using Microsoft.AspNetCore.Mvc;
using Repo.Repository.Base;
using Repo.Repository.Models;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IRepo<User> userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _userRepo.GetAllAsync();
        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userRepo.GetById(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        var createdUser = await _userRepo.Insert(user);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id)
            return BadRequest();

        await _userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userRepo.Delete(id);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
```

### 2. Controller con Paginación

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IRepo<Product> _productRepo;

    public ProductsController(IRepo<Product> productRepo)
    {
        _productRepo = productRepo;
    }

    // GET: api/products?page=1&pageSize=10&search=phone&sortBy=name&isAscending=true
    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true)
    {
        var request = new PagedRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search,
            SortBy = sortBy,
            IsAscending = isAscending,
            Filters = new Dictionary<string, object>
            {
                { "IsActive", true }
            }
        };

        var result = await _productRepo.GetPagedAsync(request);
        return Ok(result);
    }

    // GET: api/products/with-cache
    [HttpGet("with-cache")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsWithCache()
    {
        var products = await _productRepo.GetAllWithCacheAsync(TimeSpan.FromMinutes(30));
        return Ok(products);
    }
}
```

### 3. Controller con Especificaciones

```csharp
using Repo.Repository.Specifications;

[ApiController]
[Route("api/[controller]")]
public class AdvancedUsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;

    public AdvancedUsersController(IRepo<User> userRepo)
    {
        _userRepo = userRepo;
    }

    // GET: api/advanced-users/active
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<User>>> GetActiveUsers()
    {
        var spec = new ActiveUsersSpecification();
        var users = await _userRepo.GetAllBySpecAsync(spec);
        return Ok(users);
    }

    // GET: api/advanced-users/by-role/admin
    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsersByRole(string role)
    {
        var spec = new UsersByRoleSpecification(role);
        var users = await _userRepo.GetAllBySpecAsync(spec);
        return Ok(users);
    }

    // GET: api/advanced-users/search
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<User>>> SearchUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var spec = new UsersWithPaginationSpecification(page, pageSize, "Name", true);
        var request = new PagedRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = searchTerm
        };

        var result = await _userRepo.GetPagedBySpecAsync(spec, request);
        return Ok(result);
    }
}
```

## 🔧 Configuración Avanzada

### 1. Configuración con AutoMapper

```csharp
// AutoMapper Profile
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserDto, User>();
        CreateMap<Product, ProductDto>();
        CreateMap<ProductDto, Product>();
    }
}

// DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Controller con AutoMapper
[ApiController]
[Route("api/[controller]")]
public class UsersWithDtoController : ControllerBase
{
    private readonly IRepo<User> _userRepo;
    private readonly IMapper _mapper;

    public UsersWithDtoController(IRepo<User> userRepo, IMapper mapper)
    {
        _userRepo = userRepo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userRepo.GetAllAsync();
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        return Ok(userDtos);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
    {
        var user = _mapper.Map<User>(userDto);
        var createdUser = await _userRepo.Insert(user);
        var createdUserDto = _mapper.Map<UserDto>(createdUser);
        return Ok(createdUserDto);
    }
}
```

### 2. Configuración con Validación

```csharp
// Validadores
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}

// Controller con Validación
[ApiController]
[Route("api/[controller]")]
public class ValidatedUsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;
    private readonly ValidationService _validationService;

    public ValidatedUsersController(IRepo<User> userRepo, ValidationService validationService)
    {
        _userRepo = userRepo;
        _validationService = validationService;
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        var validator = new UserValidator();
        var isValid = await _validationService.IsValidAsync(user, validator);

        if (!isValid)
        {
            var result = await _validationService.ValidateAsync(user, validator);
            return BadRequest(result.Errors);
        }

        var createdUser = await _userRepo.Insert(user);
        return Ok(createdUser);
    }
}
```

### 3. Configuración con Caché

```csharp
[ApiController]
[Route("api/[controller]")]
public class CachedProductsController : ControllerBase
{
    private readonly IRepo<Product> _productRepo;
    private readonly ICacheService _cacheService;

    public CachedProductsController(IRepo<Product> productRepo, ICacheService cacheService)
    {
        _productRepo = productRepo;
        _cacheService = cacheService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        // Obtener con caché por 30 minutos
        var product = await _productRepo.GetByIdWithCacheAsync(id, TimeSpan.FromMinutes(30));

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
    {
        // Obtener todos con caché por 1 hora
        var products = await _productRepo.GetAllWithCacheAsync(TimeSpan.FromHours(1));
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var createdProduct = await _productRepo.Insert(product);

        // Invalidar caché después de crear
        await _productRepo.InvalidateCacheAsync("*");

        return Ok(createdProduct);
    }
}
```

## 📝 Ejemplos Prácticos

### 1. API Completa de E-commerce

```csharp
// Order Entity
public class Order : IAuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Order Controller
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IRepo<Order> _orderRepo;
    private readonly IRepo<Product> _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public OrdersController(
        IRepo<Order> orderRepo,
        IRepo<Product> productRepo,
        IUnitOfWork unitOfWork)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Order>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var request = new PagedRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = status != null ? new Dictionary<string, object> { { "Status", status } } : null
        };

        var result = await _orderRepo.GetPagedAsync(request);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        // Validar stock de productos
        foreach (var item in order.Items)
        {
            var product = await _productRepo.GetById(item.ProductId);
            if (product.Stock < item.Quantity)
                return BadRequest($"Producto {product.Name} no tiene suficiente stock");
        }

        // Actualizar stock
        foreach (var item in order.Items)
        {
            var product = await _productRepo.GetById(item.ProductId);
            product.Stock -= item.Quantity;
            await _productRepo.UpdateAsync(product);
        }

        var createdOrder = await _orderRepo.Insert(order);
        await _unitOfWork.SaveChangesAsync();

        return Ok(createdOrder);
    }
}
```

### 2. API con Filtros Dinámicos

```csharp
[ApiController]
[Route("api/[controller]")]
public class DynamicProductsController : ControllerBase
{
    private readonly IRepo<Product> _productRepo;

    public DynamicProductsController(IRepo<Product> productRepo)
    {
        _productRepo = productRepo;
    }

    [HttpGet("filter")]
    public async Task<ActionResult<PagedResult<Product>>> FilterProducts(
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minStock = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filters = new Dictionary<string, object>();

        if (minPrice.HasValue)
            filters.Add("Price >= @0", minPrice.Value);

        if (maxPrice.HasValue)
            filters.Add("Price <= @0", maxPrice.Value);

        if (minStock.HasValue)
            filters.Add("Stock >= @0", minStock.Value);

        if (isActive.HasValue)
            filters.Add("IsActive", isActive.Value);

        var request = new PagedRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search,
            Filters = filters
        };

        var result = await _productRepo.GetPagedAsync(request);
        return Ok(result);
    }
}
```

## 🎯 Mejores Prácticas

### 1. Estructura de Proyecto Recomendada

```
YourApi/
├── Controllers/
│   ├── UsersController.cs
│   ├── ProductsController.cs
│   └── OrdersController.cs
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Product.cs
│   │   └── Order.cs
│   ├── DTOs/
│   │   ├── UserDto.cs
│   │   ├── ProductDto.cs
│   │   └── OrderDto.cs
│   └── ViewModels/
│       ├── CreateUserRequest.cs
│       └── UpdateUserRequest.cs
├── Services/
│   ├── IUserService.cs
│   ├── UserService.cs
│   └── EmailService.cs
├── Validators/
│   ├── UserValidator.cs
│   └── ProductValidator.cs
├── Mappings/
│   └── MappingProfile.cs
├── Data/
│   └── YourDbContext.cs
└── Program.cs
```

### 2. Patrón Service Layer

```csharp
// IUserService.cs
public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task DeleteUserAsync(int id);
    Task<PagedResult<User>> GetUsersPagedAsync(PagedRequest request);
}

// UserService.cs
public class UserService : IUserService
{
    private readonly IRepo<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ValidationService _validationService;

    public UserService(
        IRepo<User> userRepo,
        IUnitOfWork unitOfWork,
        ValidationService validationService)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepo.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepo.GetById(id);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        var validator = new UserValidator();
        var isValid = await _validationService.IsValidAsync(user, validator);

        if (!isValid)
            throw new ValidationException("Usuario inválido");

        var createdUser = await _userRepo.Insert(user);
        await _unitOfWork.SaveChangesAsync();

        return createdUser;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        var validator = new UserValidator();
        var isValid = await _validationService.IsValidAsync(user, validator);

        if (!isValid)
            throw new ValidationException("Usuario inválido");

        var updatedUser = await _userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return updatedUser;
    }

    public async Task DeleteUserAsync(int id)
    {
        await _userRepo.Delete(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResult<User>> GetUsersPagedAsync(PagedRequest request)
    {
        return await _userRepo.GetPagedAsync(request);
    }
}

// Controller simplificado
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        try
        {
            var createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

### 3. Manejo de Errores Global

```csharp
// Program.cs
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error no manejado: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno del servidor",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

## 🚨 Troubleshooting

### Problemas Comunes y Soluciones

#### 1. Error de Dependencias

```bash
# Error: No se puede resolver IRepo<>
# Solución: Verificar que la librería esté correctamente referenciada
dotnet restore
dotnet clean
dotnet build
```

#### 2. Error de Entity Framework

```csharp
// Error: DbContext no configurado
// Solución: Agregar en Program.cs
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

#### 3. Error de AutoMapper

```csharp
// Error: No se puede mapear entre tipos
// Solución: Configurar AutoMapper en Program.cs
builder.Services.AddAutoMapper(typeof(Program));
```

#### 4. Error de Redis Cache

```csharp
// Error: Redis no disponible
// Solución: Configurar Redis o usar caché en memoria
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// O usar caché en memoria como fallback
builder.Services.AddMemoryCache();
```

#### 5. Error de Validación

```csharp
// Error: Validadores no registrados
// Solución: Configurar FluentValidation en Program.cs
builder.Services.AddFluentValidationAutoValidation();
```

## 📚 Recursos Adicionales

- [Documentación de Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Documentación de AutoMapper](https://docs.automapper.org/)
- [Documentación de FluentValidation](https://docs.fluentvalidation.net/)
- [Documentación de Redis](https://redis.io/documentation)
- [Patrones de Repositorio](https://martinfowler.com/eaaCatalog/repository.html)

## 🎯 Próximos Pasos

1. **Implementar Tests**: Agregar tests unitarios y de integración
2. **Configurar Logging**: Implementar logging estructurado
3. **Monitoreo**: Agregar métricas y monitoreo de rendimiento
4. **Seguridad**: Implementar autenticación y autorización
5. **Documentación API**: Generar documentación con Swagger/OpenAPI
