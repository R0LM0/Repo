# ⚡ FASE 3: OPTIMIZACIÓN DE PERFORMANCE

## Meta
Implementar optimizaciones clave de rendimiento sin cambiar comportamiento público.

## Tiempo Estimado
2-3 horas

## Issues

### Issue #13: Optimizar Paginación (Single Round-Trip)
**Archivo:** `Repository/Base/RepoBase.Pagination.cs`

**Tareas:**
- [ ] Reducir a single round-trip si es posible
- [ ] Optimizar cálculo de TotalCount
- [ ] Agregar `AsSplitQuery` opcional
- [ ] Benchmark de mejora

---

### Issue #14: Implementar AsSplitQuery para Includes Múltiples
**Archivo:** `Repository/Specifications/SpecificationEvaluator.cs`
**Archivo:** `Repository/Base/RepoBase.Queries.cs`

**Tareas:**
- [ ] Agregar propiedad `UseSplitQuery` a `ISpecification<T>`
- [ ] Implementar lógica en `SpecificationEvaluator`
- [ ] Aplicar automáticamente cuando hay >1 include
- [ ] Test de reducción de data transfer

---

### Issue #15: Optimizar Delete sin Cargar Entidades
**Archivo:** `Repository/Base/RepoBase.Core.cs`
**Archivo:** `Repository/Base/HighPerformanceRepo.cs`

**Tareas:**
- [ ] Usar `ExecuteDeleteAsync` (EF Core 7+) para deletes por predicate
- [ ] Optimizar `DeleteAsync` para no cargar entidad completa
- [ ] Test de reducción de memoria

---

### Issue #16: Implementar IAsyncEnumerable para GetAll
**Archivo:** `Repository/Base/RepoBase.Core.cs`

**Tareas:**
- [ ] Agregar `GetAllStreamAsync()` que retorne `IAsyncEnumerable<T>`
- [ ] Evitar `ToList()` en memoria
- [ ] Test con tablas grandes

---

### Issue #17: Fix CompiledQueries Thread-Safety
**Archivo:** `Repository/Base/CompiledQueries.cs`

**Tareas:**
- [ ] Corregir inicialización thread-safe
- [ ] Manejar fallos de inicialización
- [ ] Evitar reintentos infinitos

---

### Issue #18: Implementar RemoveByPatternAsync en Redis
**Archivo:** `Repository/Services/RedisCacheService.cs`

**Tareas:**
- [ ] Implementar eliminación por patrón usando StackExchange.Redis
- [ ] Test de eliminación múltiple
- [ ] Manejo de errores

---

## Criterios de Aceptación
- [ ] Mejora medible en benchmarks
- [ ] Todos los tests existentes pasan
- [ ] No hay regresiones de funcionalidad
- [ ] Documentación de cambios de performance

## PR
Título: `[PERF] Fase 3: Optimizaciones de rendimiento`
Branch: `perf/phase-3-performance`
