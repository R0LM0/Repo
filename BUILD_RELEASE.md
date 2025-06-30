# 🚀 Build en Release - Librería de Repositorio .NET 9

## 📋 Comandos para Build en Release

### 1. Build Básico en Release

```bash
# Build en Release (configuración por defecto)
dotnet build

# Build explícito en Release
dotnet build --configuration Release

# Build con optimizaciones máximas
dotnet build --configuration Release --verbosity normal
```

### 2. Clean y Rebuild

```bash
# Limpiar build anterior
dotnet clean

# Clean y rebuild en Release
dotnet clean --configuration Release
dotnet build --configuration Release
```

### 3. Build con Restore de Dependencias

```bash
# Restaurar dependencias y build en Release
dotnet restore
dotnet build --configuration Release
```

### 4. Build para Publicación

```bash
# Build optimizado para publicación
dotnet publish --configuration Release --output ./publish

# Build con configuración específica
dotnet publish --configuration Release --framework net9.0 --output ./publish
```

### 5. Generar Paquete NuGet

```bash
# Generar paquete .nupkg
dotnet pack --configuration Release

# Generar paquete con configuración específica
dotnet pack --configuration Release --output ./nupkgs
```

## 🔧 Configuraciones de Build

### Configuración Release (automática)

- ✅ **Optimizaciones habilitadas**: `Optimize=true`
- ✅ **Debug symbols deshabilitados**: `DebugSymbols=false`
- ✅ **Documentación XML generada**: `GenerateDocumentationFile=true`
- ✅ **Warnings como errores**: `TreatWarningsAsErrors=true`

### Configuración Debug (cuando se especifica)

- 🔍 **Optimizaciones deshabilitadas**: `Optimize=false`
- 🔍 **Debug symbols habilitados**: `DebugSymbols=true`
- 🔍 **DebugType full**: Para debugging completo

## 📦 Publicación como Paquete NuGet

### 1. Generar Paquete Local

```bash
# Generar paquete .nupkg
dotnet pack --configuration Release

# El paquete se genera en: bin/Release/net9.0/AdvancedRepository.NET9.1.0.0.nupkg
```

### 2. Publicar a NuGet.org

```bash
# Publicar a NuGet.org (requiere API key)
dotnet nuget push bin/Release/net9.0/AdvancedRepository.NET9.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### 3. Publicar a Fuente Local

```bash
# Publicar a fuente local de NuGet
dotnet nuget push bin/Release/net9.0/AdvancedRepository.NET9.1.0.0.nupkg --source C:\LocalNuGet
```

## 🎯 Scripts de Automatización

### PowerShell Script para Build Completo

```powershell
# build-release.ps1
Write-Host "🧹 Limpiando build anterior..." -ForegroundColor Yellow
dotnet clean --configuration Release

Write-Host "📦 Restaurando dependencias..." -ForegroundColor Yellow
dotnet restore

Write-Host "🔨 Compilando en Release..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity normal

Write-Host "📋 Generando paquete NuGet..." -ForegroundColor Yellow
dotnet pack --configuration Release --output ./nupkgs

Write-Host "✅ Build completado exitosamente!" -ForegroundColor Green
Write-Host "📁 Paquete generado en: ./nupkgs/" -ForegroundColor Cyan
```

### Batch Script para Windows

```batch
@echo off
REM build-release.bat
echo 🧹 Limpiando build anterior...
dotnet clean --configuration Release

echo 📦 Restaurando dependencias...
dotnet restore

echo 🔨 Compilando en Release...
dotnet build --configuration Release --verbosity normal

echo 📋 Generando paquete NuGet...
dotnet pack --configuration Release --output ./nupkgs

echo ✅ Build completado exitosamente!
echo 📁 Paquete generado en: ./nupkgs/
pause
```

## 🔍 Verificación del Build

### 1. Verificar Archivos Generados

```bash
# Listar archivos generados
ls bin/Release/net9.0/

# Verificar DLL optimizada
file bin/Release/net9.0/Repo.dll
```

### 2. Verificar Paquete NuGet

```bash
# Verificar contenido del paquete
dotnet nuget locals all --list

# Instalar paquete local para testing
dotnet add package AdvancedRepository.NET9 --source ./nupkgs
```

### 3. Análisis de Rendimiento

```bash
# Analizar tamaño del assembly
dotnet tool install -g dotnet-size
dotnet size bin/Release/net9.0/Repo.dll
```

## 🚨 Troubleshooting

### Problemas Comunes

1. **Error de compilación en Release**

   ```bash
   # Verificar warnings como errores
   dotnet build --configuration Release --verbosity detailed
   ```

2. **Paquete NuGet no se genera**

   ```bash
   # Verificar metadata del paquete
   dotnet pack --configuration Release --verbosity detailed
   ```

3. **Dependencias faltantes**
   ```bash
   # Restaurar dependencias explícitamente
   dotnet restore --force
   ```

## 📊 Comparación Debug vs Release

| Aspecto            | Debug             | Release        |
| ------------------ | ----------------- | -------------- |
| **Optimizaciones** | ❌ Deshabilitadas | ✅ Habilitadas |
| **Debug Symbols**  | ✅ Incluidos      | ❌ Excluidos   |
| **Tamaño**         | 🔴 Más grande     | 🟢 Más pequeño |
| **Rendimiento**    | 🔴 Más lento      | 🟢 Más rápido  |
| **Debugging**      | ✅ Completo       | ❌ Limitado    |

## 🎯 Próximos Pasos

1. **Configurar CI/CD**: Automatizar builds en GitHub Actions o Azure DevOps
2. **Versionado automático**: Implementar versionado semántico automático
3. **Testing**: Agregar tests unitarios y de integración
4. **Documentación**: Generar documentación automática con DocFX
5. **Análisis de código**: Configurar SonarQube o CodeQL
