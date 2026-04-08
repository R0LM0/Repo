# 🧪 FASE 2: TESTING CRÍTICO

## Meta
Alcanzar >80% cobertura en operaciones core y módulos críticos.

## Tiempo Estimado
3-4 horas

## Issues

### Issue #7: Tests para RepoBase Core CRUD
**Archivo:** Nuevo `Repo.Tests/Base/RepoBaseCoreTests.cs`

**Tareas:**
- [ ] Test `Add()` con persist=true/false
- [ ] Test `Update()` con persist
- [ ] Test `Delete()` con persist
- [ ] Test `Save()` y `SaveAsync()`
- [ ] Test `Insert()` async
- [ ] Test `UpdateAsync()`
- [ ] Test casos edge (nulls)

---

### Issue #8: Tests para Stored Procedures
**Archivo:** Nuevo `Repo.Tests/StoredProcedures/RepoBaseStoredProcsTests.cs`

**Tareas:**
- [ ] Test `ExecuteStoredProcedureAsync` con whitelist
- [ ] Test rechazo cuando no está en whitelist
- [ ] Test `ExecuteStoredProcedureNonQueryAsync`
- [ ] Test `ExecuteScalarFunctionAsync`
- [ ] Test `ExecuteTableValuedFunctionAsync`
- [ ] Test validación de parámetros vacíos

---

### Issue #9: Tests para UnitOfWork Completo
**Archivo:** `Repo.Tests/UnitOfWork/UnitOfWorkCompleteTests.cs`

**Tareas:**
- [ ] Test `BeginTransactionAsync(IsolationLevel)`
- [ ] Test `SaveChangesAsync()`
- [ ] Test `HasChangesAsync()`
- [ ] Test `HasActiveTransaction`
- [ ] Test `CurrentTransaction`
- [ ] Test `ExecuteSqlRawAsync()` (ambos overloads)

---

### Issue #10: Tests para Bulk Operations
**Archivo:** Nuevo `Repo.Tests/Base/RepoBaseBulkTests.cs`

**Tareas:**
- [ ] Test `AddRangeAsync()`
- [ ] Test `UpdateRangeAsync()`
- [ ] Test `DeleteRangeAsync()` con entities
- [ ] Test `DeleteRangeAsync()` con predicate
- [ ] Test casos edge (colecciones vacías)

---

### Issue #11: Tests para Queries
**Archivo:** Nuevo `Repo.Tests/Base/RepoBaseQueriesTests.cs`

**Tareas:**
- [ ] Test `AnyAsync()`
- [ ] Test `CountAsync()`
- [ ] Test `FindAsync()` con includes
- [ ] Test `FirstOrDefaultAsync()`

---

### Issue #12: Tests de Edge Cases y Excepciones
**Archivo:** Nuevo `Repo.Tests/Base/RepoBaseEdgeCasesTests.cs`

**Tareas:**
- [ ] Test operaciones con IDs inválidos (0, -1)
- [ ] Test operaciones con entidades null
- [ ] Test `DbUpdateException` handling
- [ ] Test `OperationCanceledException`
- [ ] Test comportamiento cuando contexto está disposed

---

## Criterios de Aceptación
- [ ] Cobertura de RepoBase.Core >80%
- [ ] Cobertura de StoredProcedures >80%
- [ ] Cobertura de UnitOfWork >80%
- [ ] Todos los tests pasan
- [ ] No hay tests frágiles (sin timing dependiente)

## PR
Título: `[TEST] Fase 2: Tests críticos faltantes`
Branch: `test/phase-2-critical-tests`
