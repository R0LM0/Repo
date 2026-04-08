# 🚨 FASE 1: SEGURIDAD Y ESTABILIDAD

## Meta
Corregir todas las vulnerabilidades críticas y problemas de estabilidad antes de continuar.

## Tiempo Estimado
2-3 horas

## Issues a Resolver

### Issue #1: SQL Injection en UnitOfWork.ExecuteSqlRawAsync
**Archivo:** `Repository/UnitOfWork/UnitOfWork.cs` (líneas 152-181)
**Severidad:** 🔴 CRÍTICA

**Problema:**
Los métodos `ExecuteSqlRawAsync` exponen SQL directo sin validación de whitelist.

**Tareas:**
- [ ] Agregar validación de SQL permitido (whitelist)
- [ ] No loggear SQL completo en producción
- [ ] Agregar tests de seguridad

**Código a implementar:**
```csharp
public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
{
    if (!IsSafeSql(sql))
        throw new SecurityException("SQL no permitido");
    
    _logger.LogInformation("Executed SQL with {ParameterCount} parameters", parameters.Length);
    return await _context.Database.ExecuteSqlRawAsync(sql, parameters).ConfigureAwait(false);
}
```

---

### Issue #2: SQL Injection en DatabaseFunctionExtensions
**Archivo:** `Repository/Extensions/DatabaseFunctionExtensions.cs` (líneas 74-125)
**Severidad:** 🔴 CRÍTICA

**Problema:**
Concatenación directa de parámetros en SQL (`aggregateFunction`, `columnName`, `windowFunction`).

**Tareas:**
- [ ] Crear whitelist de funciones permitidas
- [ ] Validar nombres de columnas con regex
- [ ] Usar parametrización segura

**Código a implementar:**
```csharp
private static readonly HashSet<string> AllowedFunctions = 
    new(StringComparer.OrdinalIgnoreCase) { "COUNT", "SUM", "AVG", "MAX", "MIN" };

if (!AllowedFunctions.Contains(aggregateFunction))
    throw new ArgumentException("Función no permitida");

if (!Regex.IsMatch(columnName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
    throw new ArgumentException("Nombre de columna inválido");
```

---

### Issue #3: Race Condition - async void en HighPerformanceRepo
**Archivo:** `Repository/Base/HighPerformanceRepo.cs` (línea 567)
**Severidad:** 🔴 CRÍTICA

**Problema:**
`async void ProcessBatchTimer` no captura excepciones y puede crashear el proceso.

**Tareas:**
- [ ] Cambiar a `async Task` con manejo de excepciones
- [ ] Agregar semáforo para evitar ejecuciones concurrentes
- [ ] Test de estabilidad

**Código a implementar:**
```csharp
private readonly SemaphoreSlim _timerSemaphore = new(1, 1);

private async void ProcessBatchTimer(object? state)
{
    if (!await _timerSemaphore.WaitAsync(0).ConfigureAwait(false))
        return;
    
    try
    {
        if (_batchQueue.Count > 0)
            await ProcessCurrentBatchAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error en ProcessBatchTimer");
    }
    finally
    {
        _timerSemaphore.Release();
    }
}
```

---

### Issue #4: Agregar ConfigureAwait(false) en TODO el proyecto
**Archivos:** Todos los archivos async
**Severidad:** 🟡 ALTA

**Tareas:**
- [ ] Buscar todos los `await` sin `ConfigureAwait(false)`
- [ ] Agregar `.ConfigureAwait(false)` a cada uno
- [ ] Verificar que no hay deadlocks

**Archivos a modificar:**
- Repository/Base/RepoBase.Core.cs
- Repository/Base/RepoBase.Queries.cs
- Repository/Base/RepoBase.Pagination.cs
- Repository/Base/RepoBase.Specifications.cs
- Repository/Base/RepoBase.SoftDelete.cs
- Repository/Base/RepoBase.Cache.cs
- Repository/Base/RepoBase.Bulk.cs
- Repository/UnitOfWork/UnitOfWork.cs
- Repository/Services/RedisCacheService.cs
- Repository/Services/MemoryCacheService.cs

---

### Issue #5: Validación de entidades en UnitOfWork.Repository<T>()
**Archivo:** `Repository/UnitOfWork/UnitOfWork.cs` (línea 51)
**Severidad:** 🟡 ALTA

**Tareas:**
- [ ] Validar que T sea una entidad registrada en DbContext
- [ ] Lanzar excepción clara si no es válida
- [ ] Test de validación

**Código:**
```csharp
public IRepo<T> Repository<T>() where T : class
{
    var entityType = _context.Model.FindEntityType(typeof(T));
    if (entityType == null)
        throw new InvalidOperationException($"El tipo {typeof(T).Name} no es una entidad válida");
    // ... resto
}
```

---

### Issue #6: Fix Cache Stampede en MemoryCacheService
**Archivo:** `Repository/Services/MemoryCacheService.cs` (líneas 124-141)
**Severidad:** 🟡 ALTA

**Tareas:**
- [ ] Implementar double-check locking por key
- [ ] Usar SemaphoreSlim por key
- [ ] Test de concurrencia

---

## Criterios de Aceptación
- [ ] Todos los tests existentes pasan
- [ ] No hay nuevas vulnerabilidades de seguridad
- [ ] No hay deadlocks en operaciones async
- [ ] Code review aprobado

## PR
Título: `[SECURITY] Fase 1: Correcciones críticas de seguridad y estabilidad`
Branch: `fix/phase-1-security-stability`
