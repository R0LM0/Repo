# ✨ FASE 4: FEATURES NUEVAS

## Meta
Agregar features que modernicen la librería y la hagan más competitiva.

## Tiempo Estimado
4-6 horas

## Issues

### Issue #19: Global Query Filters + Interceptors
**Archivo:** Nuevo `Repository/Interceptors/SoftDeleteInterceptor.cs`

**Tareas:**
- [ ] Crear `SoftDeleteInterceptor` para convertir DELETE en UPDATE
- [ ] Crear `AuditInterceptor` para timestamps automáticos
- [ ] Configurar global query filters para soft delete
- [ ] Documentación de uso
- [ ] Tests de integración

---

### Issue #20: ProjectTo/Select en Specifications
**Archivo:** `Repository/Specifications/ISpecification.cs`
**Archivo:** `Repository/Specifications/SpecificationEvaluator.cs`

**Tareas:**
- [ ] Agregar `Selector` property a specification
- [ ] Crear `ISpecification<T, TResult>`
- [ ] Implementar evaluación de proyección
- [ ] Métodos `GetAllBySpecAsync<T, TResult>()`
- [ ] Tests de proyección

---

### Issue #21: Audit Logging Completo
**Archivo:** Nuevo `Repository/Interceptors/AuditInterceptor.cs`
**Archivo:** `Repository/Models/AuditEntry.cs`

**Tareas:**
- [ ] Detectar cambios en entidades auditables
- [ ] Guardar trail de cambios (OldValues, NewValues)
- [ ] Configuración de qué entidades auditar
- [ ] Tests de auditoría

---

### Issue #22: Health Checks
**Archivo:** Nuevo `Repository/HealthChecks/RepositoryHealthCheck.cs`

**Tareas:**
- [ ] Implementar `IHealthCheck` para conexión a BD
- [ ] Implementar health check para Redis (si está configurado)
- [ ] Extension method `AddRepositoryHealthChecks()`
- [ ] Documentación

---

### Issue #23: Specification Composition (AND, OR, NOT)
**Archivo:** `Repository/Specifications/BaseSpecification.cs`

**Tareas:**
- [ ] Implementar operadores `&`, `|`, `!` para specifications
- [ ] Métodos `And()`, `Or()`, `Not()`
- [ ] Tests de composición

---

### Issue #24: Include Dinámico
**Archivo:** `Repository/Base/RepoBase.Queries.cs`

**Tareas:**
- [ ] Método `GetAllAsync` con parámetro `includes` como strings
- [ ] Parseo de paths de navegación
- [ ] Tests

---

## Criterios de Aceptación
- [ ] Nuevas features tienen tests
- [ ] Documentación actualizada
- [ ] Ejemplos de uso
- [ ] Breaking changes documentados (si aplica)

## PR
Título: `[FEATURE] Fase 4: Nuevas funcionalidades`
Branch: `feat/phase-4-new-features`
