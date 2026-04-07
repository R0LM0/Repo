# Publicación de Paquetes NuGet

## Pre-requisitos

1. Tener una cuenta en [NuGet.org](https://www.nuget.org/)
2. Generar un API Key en NuGet.org
3. Configurar el secreto `NUGET_API_KEY` en GitHub:
   - Ir a Settings → Secrets and variables → Actions
   - New repository secret
   - Name: `NUGET_API_KEY`
   - Value: Tu API Key de NuGet

## Opción 1: Publicar automáticamente con tags (Recomendado)

```bash
# Crear un tag con versión semver
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

El workflow de GitHub Actions se ejecutará automáticamente y publicará:
- `AdvancedRepository.NET8.1.0.0.nupkg`
- `AdvancedRepository.Integrations.Mapperly.1.0.0.nupkg`

## Opción 2: Publicar manualmente desde GitHub Actions

1. Ir a Actions → Publish NuGet Packages
2. Click en "Run workflow"
3. Ingresar la versión (ej: `1.0.0` o `1.0.0-beta1`)
4. Click en "Run workflow"

## Opción 3: Publicar localmente (desarrollo/testing)

```bash
# Pack core package
dotnet pack Repo.csproj --configuration Release -p:PackageVersion=1.0.0-beta1 -o ./packages

# Pack Mapperly integration
dotnet pack Integrations/Repo.Integrations.Mapperly/Repo.Integrations.Mapperly.csproj --configuration Release -p:PackageVersion=1.0.0-beta1 -o ./packages

# Publicar a NuGet (requiere API Key configurada)
dotnet nuget push ./packages/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Versionado SemVer

- **MAJOR**: Breaking changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes

Ejemplos:
- `1.0.0` - Release inicial
- `1.1.0` - Nueva feature
- `1.1.1` - Bug fix
- `2.0.0-beta1` - Pre-release de breaking changes

## Verificar publicación

Después de publicar, verificar en:
https://www.nuget.org/packages/AdvancedRepository.NET8/
https://www.nuget.org/packages/AdvancedRepository.Integrations.Mapperly/

## Notas

- El PackageId del core es `AdvancedRepository.NET8` (se mantiene por compatibilidad histórica aunque soporte net9.0)
- El PackageId de la integración es `AdvancedRepository.Integrations.Mapperly`
- Ambos paquetes usan la misma versión simultáneamente
