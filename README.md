# AdvancedRepository.NET

A production-grade repository pattern library for .NET 8 and .NET 9 with high-performance operations, caching, specifications, and optional compile-time mapping.

## Supported Target Frameworks

- **.NET 8.0** (LTS) — Full feature support with EF Core 8.0.12
- **.NET 9.0** — Full feature support with EF Core 9.0.2

## What's New / Current Capabilities

### Recently Hardened (Production-Ready)

- **Real Multi-Targeting** — Native support for both .NET 8.0 LTS and .NET 9.0 with framework-aligned dependencies
- **Consistent Tracking Behavior** — Unified `asNoTracking` behavior across specifications and pagination queries
- **Comprehensive Test Coverage** — Soft delete, cache integration, high-performance paths, and tracking scenarios fully tested
- **Clean Transaction APIs** — Obsolete repository-level transaction methods removed; clearer unsupported search behavior
- **Optional Mapperly Integration** — Compile-time mapping support via separate integration package (`Repo.Integrations.Mapperly`)

## Architecture Overview

```
Core Package (AdvancedRepository.NET8)
├── Repository.Base
│   ├── IRepo<T> — Full CRUD, soft delete, cache, specifications, bulk ops
│   ├── IHighPerformanceRepo<T> — Optimized bulk/streaming for high-throughput
│   └── UnitOfWork — Transaction orchestration
├── Repository.Specifications — Reusable query specifications
├── Repository.Services — Redis cache, FluentValidation
└── Repository.Models — PagedResult, performance metrics

Optional Integration
└── Repo.Integrations.Mapperly — Compile-time DTO mapping (Riok.Mapperly 4.1.0)
```

> **Note:** AutoMapper is no longer part of the core. Use the optional Mapperly integration for compile-time mapping, or bring your own mapper.

## Installation

### Core Package

```bash
dotnet add package AdvancedRepository.NET8
```

### Optional: Mapperly Integration

```bash
dotnet add package AdvancedRepository.Integrations.Mapperly
```

## Quick Start

### 1. Configure Services

```csharp
// Program.cs
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Core repository
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();

// Optional: Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Optional: Mapperly mappers (stateless, thread-safe)
builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<ProductMapper>();
```

### 2. Define Your Entity

```csharp
public class User : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 3. Create Mapper (Optional)

```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User entity);
    public partial IEnumerable<UserDto> ToDtoCollection(IEnumerable<User> entities);
}
```

### 4. Use in Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;
    private readonly UserMapper _mapper;

    public UsersController(IRepo<User> userRepo, UserMapper mapper)
    {
        _userRepo = userRepo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(
        [FromQuery] bool noTracking = false)
    {
        var users = await _userRepo.GetAllAsync(noTracking);
        return Ok(_mapper.ToDtoCollection(users));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _userRepo.GetById(id);
        if (user == null) return NotFound();
        return Ok(_mapper.ToDto(user));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool noTracking = true) // Optimized for read-only
    {
        var result = await _userRepo.GetPagedAsync(
            new PagedRequest { PageNumber = page, PageSize = pageSize },
            noTracking);
        
        return Ok(new PagedResult<UserDto>
        {
            Data = _mapper.ToDtoCollection(result.Data),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        });
    }
}
```

## Recommended Usage in a New API Project

### Project Structure

```
MyApi/
├── MyApi.Core/              # Domain entities, interfaces
│   ├── Entities/
│   └── DTOs/
├── MyApi.Infrastructure/    # DbContext, migrations
├── MyApi.Application/       # Services, mappers, specifications
│   ├── Mappers/            # Mapperly mappers
│   ├── Specifications/     # Query specifications
│   └── Services/
└── MyApi.Web/              # Controllers, Program.cs
    └── Controllers/
```

### Dependency Injection Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repository Core
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<AppDbContext>>();

// Optional: Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Application Services
builder.Services.AddScoped<IUserService, UserService>();

// Mapperly Mappers (singleton, thread-safe)
builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<ProductMapper>();
builder.Services.AddSingleton<OrderMapper>();

// API
builder.Services.AddControllers();
```

### Specification Pattern Example

```csharp
// Specifications/ActiveUsersSpecification.cs
public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
    {
        AddCriteria(u => !u.IsDeleted);
        AddOrderBy(u => u.Name);
        IsTrackingEnabled = false; // Optimize for reads
    }
}

// Usage in service
public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
{
    var spec = new ActiveUsersSpecification();
    var users = await _userRepo.GetAllBySpecAsync(spec);
    return _mapper.ToDtoCollection(users);
}
```

## Performance Notes

| Feature | Best For | Tracking Strategy |
|---------|----------|-------------------|
| `GetAllAsync(asNoTracking: true)` | List views, exports | No tracking |
| `GetPagedAsync(asNoTracking: true)` | Data grids, APIs | No tracking |
| `GetBySpecAsync(spec)` | Filtered queries | Per-spec via `IsTrackingEnabled` |
| `FindAsync(predicate, asNoTracking: true)` | Search results | No tracking |
| `GetById` / `Insert` / `Update` | Entity mutations | Tracking required |

## Breaking Changes from Previous Versions

### AutoMapper Removed from Core
- **Before**: `MappingService` and AutoMapper included in core package
- **After**: Use `Repo.Integrations.Mapperly` for compile-time mapping, or bring your own mapper
- **Migration**: Replace `MappingService` usage with direct Mapperly mapper injection

### Repository Transaction APIs Removed
- **Before**: `IRepo.BeginTransaction()`, `CommitTransaction()`, `RollbackTransaction()`
- **After**: Use `IUnitOfWork` for all transaction orchestration
- **Migration**: Replace repository transaction calls with unit of work pattern

### Multi-Target Support
- **Before**: Single target `net8.0`
- **After**: Multi-target `net8.0;net9.0`
- **Migration**: No code changes required; package automatically selects appropriate framework

## Key Features

### CRUD Operations
- Full sync/async CRUD with `Find`, `GetAll`, `Insert`, `Update`, `Delete`
- Bulk operations: `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`

### Soft Delete
- `ISoftDelete` interface for logical deletion
- `SoftDeleteAsync`, `RestoreAsync`, `GetAllIncludingDeletedAsync`

### Specifications
- Reusable query specifications with `BaseSpecification<T>`
- Composition with criteria, includes, ordering
- Automatic `AsNoTracking` respect via `IsTrackingEnabled`

### Pagination
- `PagedResult<T>` with metadata (total count, page info, navigation flags)
- `GetPagedAsync` with optional `asNoTracking` parameter

### Caching
- `ICacheService` abstraction (Redis implementation included)
- `GetByIdWithCacheAsync`, `GetAllWithCacheAsync`, `InvalidateCacheAsync`

### High Performance
- `IHighPerformanceRepo<T>` for bulk/streaming scenarios
- `IAsyncEnumerable<T>` support for large datasets

### Unit of Work
- Transaction orchestration across multiple repositories
- Consistent `SaveChangesAsync` boundary

## Documentation

- [Mapperly Integration Guide](Integrations/Repo.Integrations.Mapperly/README.md)
- Architecture decisions and patterns: See `/docs` (coming in #47)
- End-to-end sample API: See issue #45

## License

MIT — See [LICENSE](LICENSE) for details.

---

**Built for production APIs. Optimized for performance. Designed for maintainability.**
