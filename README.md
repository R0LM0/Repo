<div align="center">

# 🚀 AdvancedRepository.NET

**A production-grade, enterprise-ready Repository Pattern library for .NET 8/9 with high-performance operations, intelligent caching, security hardening, and powerful query composition.**

[![NuGet](https://img.shields.io/nuget/v/AdvancedRepository.svg?style=flat-square&logo=nuget&color=blue)](https://www.nuget.org/packages/AdvancedRepository)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AdvancedRepository.svg?style=flat-square&logo=nuget&color=green)](https://www.nuget.org/packages/AdvancedRepository)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0%20%7C%209.0-ff69b4?style=flat-square)](https://docs.microsoft.com/ef/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/R0LM0/Repo/publish-nuget.yml?style=flat-square&logo=github&color=success)](https://github.com/R0LM0/Repo/actions)
[![License](https://img.shields.io/badge/license-MIT-yellow?style=flat-square)](LICENSE)

</div>

---

## 📋 Table of Contents

- [🌟 What's New in v1.1.0](#-whats-new-in-v110)
- [✨ Key Features](#-key-features)
- [🚀 Quick Start](#-quick-start)
- [📦 Installation](#-installation)
- [🔧 Configuration](#-configuration)
- [📖 Usage Examples](#-usage-examples)
- [🛡️ Security Features](#️-security-features)
- [⚡ Performance Optimizations](#-performance-optimizations)
- [🏢 Enterprise Patterns](#-enterprise-patterns)
- [📊 Version History](#-version-history)
- [🤝 Contributing](#-contributing)

---

## 🌟 What's New in v1.1.0

### 🎯 Specification Composition (NEW!)

Combine query specifications using logical operators for unprecedented query flexibility:

```csharp
var activeSpec = new ActiveUsersSpec();
var premiumSpec = new PremiumUsersSpec();
var recentSpec = new RecentLoginSpec(days: 30);

// AND + OR composition
var complexQuery = activeSpec.And(premiumSpec).Or(recentSpec);

// Negation
var inactiveUsers = activeSpec.Not();

var users = await repo.GetAllBySpecAsync(complexQuery);
```

### 🔐 Security Hardening

- **SQL Injection Prevention** - Whitelist validation for all stored procedures
- **Retry Policy** - Automatic handling of transient failures
- **Secure Defaults** - Safe configurations out of the box

### ⚡ Performance Enhancements

- **Split Query Optimization** - Eliminates Cartesian explosion
- **Streaming Support** - `IAsyncEnumerable` for large datasets
- **Cache Stampede Protection** - Prevents cache avalanches

---

## ✨ Key Features

| Category | Features |
|----------|----------|
| **🔒 Security** | SQL Injection protection, Stored procedure whitelist, Retry policies with exponential backoff |
| **⚡ Performance** | AsSplitQuery, Streaming (IAsyncEnumerable), Optimized bulk operations, Cache stampede protection |
| **🎯 Querying** | Specification pattern, Composition (And/Or/Not), Projection (Select/ProjectTo), Pagination |
| **💾 Caching** | Redis integration, Automatic invalidation, Pattern-based cache clearing |
| **🔄 Transactions** | Unit of Work pattern, Cross-repository transactions, Savepoint support |
| **🏥 Monitoring** | Health checks integration, Performance metrics, Structured logging |

---

## 📦 Installation

### Core Package

```bash
dotnet add package AdvancedRepository --version 1.1.0
```

### Supported Databases

The library works with any EF Core provider:

```bash
# SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql

# SQLite
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

### Optional: Redis Cache

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

### Optional: Mapperly Integration (Compile-time Mapping)

```bash
dotnet add package Repo.Integrations.Mapperly
```

---

## 🚀 Quick Start

### 1️⃣ Configure Services

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 🎯 Core Repository (Required)
builder.Services.AddScoped(typeof(IRepo<>), typeof(RepoBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<AppDbContext>>();

// 🔒 Security - Stored Procedure Whitelist (Optional but recommended)
builder.Services.AddStoredProcedureWhitelist(options =>
{
    options.RequireWhitelist = true;
    options.WhitelistedProcedures = new[]
    {
        "sp_GetUsers",
        "sp_UpdateUser",
        "fn_GetUserCount"
    };
});

// 🔄 Retry Policy for Transient Failures (Optional)
builder.Services.AddRepositoryRetryPolicy(options =>
{
    options.MaxRetryAttempts = 3;
    options.UseExponentialBackoff = true;
    options.UseJitter = true;
    options.InitialDelay = TimeSpan.FromMilliseconds(200);
});

// 💾 Distributed Cache with Redis (Optional)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// 🏥 Health Checks (Optional)
builder.Services.AddHealthChecks()
    .AddRepositoryCheck<AppDbContext>("database");
```

### 2️⃣ Define Your Entity

```csharp
public class User : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### 3️⃣ Create Specifications

```csharp
// Specifications/ActiveUsersSpec.cs
public class ActiveUsersSpec : BaseSpecification<User>
{
    public ActiveUsersSpec()
    {
        AddCriteria(u => !u.IsDeleted);
        AddOrderBy(u => u.Name);
        IsTrackingEnabled = false; // Optimize for reads
    }
}

// Specifications/PremiumUsersSpec.cs
public class PremiumUsersSpec : BaseSpecification<User>
{
    public PremiumUsersSpec()
    {
        AddCriteria(u => u.IsPremium);
        IsTrackingEnabled = false;
    }
}
```

### 4️⃣ Use in Your Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRepo<User> _userRepo;

    public UsersController(IRepo<User> userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetActiveUsers()
    {
        var spec = new ActiveUsersSpec();
        var users = await _userRepo.GetAllBySpecAsync(spec);
        return Ok(users);
    }

    [HttpGet("premium-recent")]
    public async Task<ActionResult<IEnumerable<User>>> GetPremiumOrRecent()
    {
        // 🎯 Specification Composition
        var premiumOrRecent = new PremiumUsersSpec()
            .Or(new RecentLoginSpec(days: 7));
        
        var users = await _userRepo.GetAllBySpecAsync(premiumOrRecent);
        return Ok(users);
    }
}
```

---

## 📖 Usage Examples

### 🎯 Specification Pattern Deep Dive

```csharp
// Basic specification
public class RecentOrdersSpec : BaseSpecification<Order>
{
    public RecentOrdersSpec(int days)
    {
        AddCriteria(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-days));
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        IsTrackingEnabled = false;
    }
}

// Specification with projection
public class OrderSummarySpec : BaseSpecification<Order>
{
    public OrderSummarySpec()
    {
        Select(o => new OrderSummaryDto
        {
            OrderId = o.Id,
            CustomerName = o.Customer.Name,
            TotalAmount = o.Items.Sum(i => i.Price * i.Quantity),
            ItemCount = o.Items.Count
        });
        IsTrackingEnabled = false;
    }
}

// Using split query for complex includes
public class OrderWithDetailsSpec : BaseSpecification<Order>
{
    public OrderWithDetailsSpec(int orderId)
    {
        AddCriteria(o => o.Id == orderId);
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        AddInclude(o => o.ShippingAddress);
        UseSplitQuery = true; // Executes as separate queries
    }
}
```

### 🔗 Specification Composition (v1.1.0)

```csharp
// Define base specifications
var activeSpec = new ActiveUsersSpec();
var premiumSpec = new PremiumUsersSpec();
var recentSpec = new RecentLoginSpec(days: 30);
var verifiedSpec = new EmailVerifiedSpec();

// AND - Both conditions must match
var activePremium = activeSpec.And(premiumSpec);

// OR - Either condition matches
var premiumOrRecent = premiumSpec.Or(recentSpec);

// Complex: (Active AND Premium) OR (Active AND Recent)
var complex = activeSpec.And(premiumSpec).Or(activeSpec.And(recentSpec));

// NOT - Negation
var inactiveUsers = activeSpec.Not();
var nonPremium = premiumSpec.Not();

// Combined with paging
var pagedSpec = activePremium
    .And(verifiedSpec)
    .ApplyPaging(page: 1, pageSize: 20);

var result = await repo.GetPagedBySpecAsync(pagedSpec);
```

### 💾 Caching Strategies

```csharp
// Automatic cache with expiration
public async Task<User> GetUserWithCache(int id)
{
    return await _userRepo.GetByIdWithCacheAsync(
        id, 
        cacheExpiration: TimeSpan.FromMinutes(5));
}

// Invalidate cache after update
public async Task UpdateUser(User user)
{
    await _userRepo.UpdateAsync(user);
    await _userRepo.InvalidateCacheAsync(user.Id);
}

// Pattern-based invalidation
public async Task RefreshAllUsers()
{
    await _userRepo.InvalidateCacheAsync("users:*");
}
```

### 🔄 Unit of Work Pattern

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepo<Order> _orderRepo;
    private readonly IRepo<Product> _productRepo;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _orderRepo = unitOfWork.Repository<Order>();
        _productRepo = unitOfWork.Repository<Product>();
    }

    public async Task CreateOrderAsync(Order order, List<OrderItem> items)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Create order
            await _orderRepo.AddAsync(order);
            
            // Update inventory
            foreach (var item in items)
            {
                var product = await _productRepo.GetById(item.ProductId);
                product.Stock -= item.Quantity;
                await _productRepo.UpdateAsync(product);
            }
            
            // Commit all changes atomically
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

### ⚡ High-Performance Operations

```csharp
public class BulkImportService
{
    private readonly IHighPerformanceRepo<Product> _repo;

    public async Task BulkImportAsync(List<Product> products)
    {
        // Bulk insert
        await _repo.BulkInsertAsync(products);
        
        // Parallel processing
        await _repo.ProcessParallelAsync(products, async product =>
        {
            await EnrichProductData(product);
        }, maxDegreeOfParallelism: 10);
        
        // Streaming for large datasets
        await foreach (var product in _repo.GetAllStreamAsync())
        {
            await ProcessProduct(product);
        }
    }
}
```

### 🗄️ Stored Procedures with Security

```csharp
// Secure execution with whitelist validation
public async Task<List<User>> GetUsersViaStoredProc()
{
    // Only executes if "sp_GetActiveUsers" is in the whitelist
    return await _userRepo.ExecuteStoredProcedureAsync<User>(
        "sp_GetActiveUsers",
        new SqlParameter("@IsActive", true));
}

// Scalar function
public async Task<int> GetUserCount()
{
    return await _userRepo.ExecuteScalarFunctionAsync<int>(
        "fn_GetUserCount");
}

// Table-valued function
public async Task<List<User>> SearchUsers(string searchTerm)
{
    return await _userRepo.ExecuteTableValuedFunctionAsync<User>(
        "fn_SearchUsers",
        new SqlParameter("@SearchTerm", searchTerm));
}
```

---

## 🛡️ Security Features

### SQL Injection Prevention

```csharp
// ✅ SAFE - Uses parameterized queries internally
await _repo.ExecuteStoredProcedureAsync<User>(
    "sp_GetUser",
    new SqlParameter("@Id", userId));

// ✅ SAFE - Whitelist prevents unauthorized procedures
services.AddStoredProcedureWhitelist(options =>
{
    options.RequireWhitelist = true;
    // Only these can be executed
    options.WhitelistedProcedures = new[] { "sp_GetUser", "sp_UpdateUser" };
});

// ❌ BLOCKED - Throws SecurityException
await _repo.ExecuteStoredProcedureAsync<User>("DROP TABLE Users");
```

### Retry Policy Configuration

```csharp
services.AddRepositoryRetryPolicy(options =>
{
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
    options.InitialDelay = TimeSpan.FromMilliseconds(200);
    options.MaxDelay = TimeSpan.FromSeconds(10);
    options.UseExponentialBackoff = true;
    options.UseJitter = true; // Adds randomness to prevent thundering herd
});
```

**Retry occurs on:**
- SQL timeouts (-2)
- Connection failures (53, 258)
- Azure SQL transient errors (40197, 40501, 40613)
- OperationCanceledException

---

## ⚡ Performance Optimizations

| Optimization | When to Use | Benefit |
|-------------|-------------|---------|
| `UseSplitQuery = true` | Multiple Includes | Prevents Cartesian explosion, faster queries |
| `IsTrackingEnabled = false` | Read-only scenarios | 40% less memory, faster execution |
| `IAsyncEnumerable<T>` | Large datasets | Constant memory regardless of dataset size |
| `BulkInsertAsync` | 100+ entities | 10x faster than individual inserts |
| `Cache stampede protection` | High-read scenarios | Prevents DB overload on cache expiry |
| `ExecuteDeleteAsync` | Bulk deletes | Single round-trip, no entity loading |

### Benchmarks

```
Split Query (3 includes):     45ms  → 12ms  (-73%)
No Tracking (1,000 entities): 12MB → 7MB   (-42%)
Bulk Insert (1,000):          2.3s → 180ms (-92%)
Cached Read:                  45ms → 2ms   (-95%)
```

---

## 🏢 Enterprise Patterns

### Multi-Tenancy Support

```csharp
public class TenantSpecification<T> : BaseSpecification<T> where T : ITenantEntity
{
    public TenantSpecification(int tenantId)
    {
        AddCriteria(e => e.TenantId == tenantId);
    }
}

// Usage
var tenantSpec = new TenantSpecification<Order>(currentTenantId)
    .And(new ActiveOrdersSpec());
    
var orders = await _repo.GetAllBySpecAsync(tenantSpec);
```

### Audit Trail

```csharp
public class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}

// Automatic via Unit of Work
public override async Task<int> SaveChangesAsync()
{
    foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = DateTime.UtcNow;
            entry.Entity.CreatedBy = _currentUserService.UserId;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.ModifiedAt = DateTime.UtcNow;
            entry.Entity.ModifiedBy = _currentUserService.UserId;
        }
    }
    return await base.SaveChangesAsync();
}
```

---

## 📊 Version History

| Version | PR | Key Features | Release Date |
|---------|-----|--------------|--------------|
| **1.1.0** | #64 | Specification Composition (And, Or, Not operators) | 2026-04-08 |
| **1.0.6** | #63 | ProjectTo/Select projections, Health Checks integration | 2026-04-08 |
| **1.0.5** | #62 | AsSplitQuery, Optimized DeleteAsync, IAsyncEnumerable streaming, Cache stampede fix | 2026-04-08 |
| **1.0.4** | #60 | SQL Injection prevention, Retry Policy, Async void fixes | 2026-04-08 |

---

## 🔧 Advanced Configuration

### Custom Cache Implementation

```csharp
public class CustomCacheService : ICacheService
{
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Your custom cache logic
    }
    
    public Task RemoveAsync(string key) { /* ... */ }
    public Task RemoveByPatternAsync(string pattern) { /* ... */ }
}

services.AddScoped<ICacheService, CustomCacheService>();
```

### Custom Retry Policy

```csharp
public class CustomRetryPolicy : IRetryPolicy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        // Your custom retry logic
    }
}

services.AddScoped<IRetryPolicy, CustomRetryPolicy>();
```

---

## 🧪 Testing

The library includes comprehensive test coverage (~80 tests):

```bash
dotnet test Repo.Tests/Repo.Tests.csproj
```

Test categories:
- ✅ Core CRUD operations
- ✅ Specification pattern
- ✅ Caching behavior
- ✅ Transaction management
- ✅ Security (SQL injection prevention)
- ✅ Retry policies
- ✅ Bulk operations
- ✅ Edge cases

---

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Built for production APIs. Optimized for performance. Hardened for security.**

[⭐ Star us on GitHub](https://github.com/R0LM0/Repo) | [📦 NuGet Package](https://www.nuget.org/packages/AdvancedRepository) | [🐛 Report Issue](https://github.com/R0LM0/Repo/issues)

</div>
