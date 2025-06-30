# 🚀 Script de Build en Release para Librería de Repositorio .NET 9
# Autor: Tu Nombre
# Fecha: $(Get-Date -Format "yyyy-MM-dd")

param(
    [switch]$Clean,
    [switch]$Pack,
    [switch]$Publish,
    [string]$Version = "1.0.0",
    [string]$OutputPath = "./nupkgs"
)

# Colores para output
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"
$Cyan = "Cyan"
$White = "White"

# Función para escribir mensajes con colores
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $White
    )
    Write-Host $Message -ForegroundColor $Color
}

# Función para verificar si el comando existe
function Test-Command {
    param([string]$Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

# Verificar que .NET CLI esté disponible
if (-not (Test-Command "dotnet")) {
    Write-ColorOutput "❌ Error: .NET CLI no está disponible en el PATH" $Red
    Write-ColorOutput "   Por favor, instala .NET 9 SDK desde: https://dotnet.microsoft.com/download" $Yellow
    exit 1
}

# Mostrar información del sistema
Write-ColorOutput "🔍 Información del Sistema:" $Cyan
Write-ColorOutput "   .NET Version: $(dotnet --version)" $White
Write-ColorOutput "   Directorio actual: $(Get-Location)" $White
Write-ColorOutput "   Versión del paquete: $Version" $White
Write-ColorOutput ""

# Función para limpiar build anterior
function Invoke-Clean {
    Write-ColorOutput "🧹 Limpiando build anterior..." $Yellow
    
    try {
        dotnet clean --configuration Release --verbosity minimal
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Limpieza completada exitosamente" $Green
        } else {
            Write-ColorOutput "⚠️ Advertencia: La limpieza no fue completamente exitosa" $Yellow
        }
    }
    catch {
        Write-ColorOutput "❌ Error durante la limpieza: $($_.Exception.Message)" $Red
        return $false
    }
    
    return $true
}

# Función para restaurar dependencias
function Invoke-Restore {
    Write-ColorOutput "📦 Restaurando dependencias..." $Yellow
    
    try {
        dotnet restore --verbosity minimal
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Dependencias restauradas exitosamente" $Green
        } else {
            Write-ColorOutput "❌ Error al restaurar dependencias" $Red
            return $false
        }
    }
    catch {
        Write-ColorOutput "❌ Error durante la restauración: $($_.Exception.Message)" $Red
        return $false
    }
    
    return $true
}

# Función para compilar en Release
function Invoke-Build {
    Write-ColorOutput "🔨 Compilando en Release..." $Yellow
    
    try {
        dotnet build --configuration Release --verbosity normal --no-restore
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Compilación completada exitosamente" $Green
        } else {
            Write-ColorOutput "❌ Error durante la compilación" $Red
            return $false
        }
    }
    catch {
        Write-ColorOutput "❌ Error durante la compilación: $($_.Exception.Message)" $Red
        return $false
    }
    
    return $true
}

# Función para generar paquete NuGet
function Invoke-Pack {
    Write-ColorOutput "📋 Generando paquete NuGet..." $Yellow
    
    # Crear directorio de salida si no existe
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-ColorOutput "📁 Directorio de salida creado: $OutputPath" $Cyan
    }
    
    try {
        dotnet pack --configuration Release --output $OutputPath --no-build --verbosity normal
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Paquete NuGet generado exitosamente" $Green
            
            # Mostrar información del paquete generado
            $nupkgFiles = Get-ChildItem -Path $OutputPath -Filter "*.nupkg"
            foreach ($file in $nupkgFiles) {
                $fileSize = [math]::Round($file.Length / 1KB, 2)
                Write-ColorOutput "   📦 $($file.Name) ($fileSize KB)" $Cyan
            }
        } else {
            Write-ColorOutput "❌ Error al generar paquete NuGet" $Red
            return $false
        }
    }
    catch {
        Write-ColorOutput "❌ Error durante el empaquetado: $($_.Exception.Message)" $Red
        return $false
    }
    
    return $true
}

# Función para publicar (opcional)
function Invoke-Publish {
    Write-ColorOutput "🚀 Publicando paquete..." $Yellow
    
    $nupkgFiles = Get-ChildItem -Path $OutputPath -Filter "*.nupkg"
    if ($nupkgFiles.Count -eq 0) {
        Write-ColorOutput "❌ No se encontraron paquetes para publicar" $Red
        return $false
    }
    
    foreach ($file in $nupkgFiles) {
        Write-ColorOutput "   📤 Publicando $($file.Name)..." $Cyan
        
        # Aquí puedes agregar la lógica para publicar a NuGet.org
        # dotnet nuget push $file.FullName --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
        
        Write-ColorOutput "   ⚠️ Publicación manual requerida" $Yellow
        Write-ColorOutput "   💡 Comando sugerido:" $Cyan
        Write-ColorOutput "      dotnet nuget push $($file.FullName) --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" $White
    }
    
    return $true
}

# Función para verificar el build
function Invoke-Verify {
    Write-ColorOutput "🔍 Verificando build..." $Yellow
    
    $releasePath = "bin/Release/net9.0"
    if (Test-Path $releasePath) {
        $files = Get-ChildItem -Path $releasePath
        Write-ColorOutput "   📁 Archivos generados en $releasePath:" $Cyan
        
        foreach ($file in $files) {
            $fileSize = [math]::Round($file.Length / 1KB, 2)
            Write-ColorOutput "      📄 $($file.Name) ($fileSize KB)" $White
        }
        
        # Verificar DLL principal
        $dllFile = Join-Path $releasePath "Repo.dll"
        if (Test-Path $dllFile) {
            Write-ColorOutput "   ✅ DLL principal encontrada" $Green
        } else {
            Write-ColorOutput "   ❌ DLL principal no encontrada" $Red
        }
    } else {
        Write-ColorOutput "   ❌ Directorio de Release no encontrado" $Red
    }
}

# Función para mostrar estadísticas
function Show-Statistics {
    Write-ColorOutput "📊 Estadísticas del Build:" $Cyan
    
    $startTime = Get-Date
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-ColorOutput "   ⏱️ Tiempo total: $($duration.TotalSeconds.ToString('F2')) segundos" $White
    Write-ColorOutput "   📦 Paquetes generados: $(Get-ChildItem -Path $OutputPath -Filter '*.nupkg').Count" $White
    
    # Tamaño total de archivos generados
    $releasePath = "bin/Release/net9.0"
    if (Test-Path $releasePath) {
        $totalSize = (Get-ChildItem -Path $releasePath -Recurse | Measure-Object -Property Length -Sum).Sum
        $totalSizeKB = [math]::Round($totalSize / 1KB, 2)
        Write-ColorOutput "   💾 Tamaño total: $totalSizeKB KB" $White
    }
}

# Función principal
function Main {
    Write-ColorOutput "🚀 Iniciando Build en Release - Librería de Repositorio .NET 9" $Cyan
    Write-ColorOutput "=" * 60 $White
    Write-ColorOutput ""
    
    $startTime = Get-Date
    $success = $true
    
    # Limpiar si se especifica
    if ($Clean -or $Pack -or $Publish) {
        $success = Invoke-Clean
        if (-not $success) {
            Write-ColorOutput "❌ Falló la limpieza. Abortando build." $Red
            return
        }
    }
    
    # Restaurar dependencias
    $success = Invoke-Restore
    if (-not $success) {
        Write-ColorOutput "❌ Falló la restauración. Abortando build." $Red
        return
    }
    
    # Compilar
    $success = Invoke-Build
    if (-not $success) {
        Write-ColorOutput "❌ Falló la compilación. Abortando build." $Red
        return
    }
    
    # Generar paquete si se especifica
    if ($Pack -or $Publish) {
        $success = Invoke-Pack
        if (-not $success) {
            Write-ColorOutput "❌ Falló la generación del paquete. Abortando build." $Red
            return
        }
    }
    
    # Publicar si se especifica
    if ($Publish) {
        $success = Invoke-Publish
        if (-not $success) {
            Write-ColorOutput "❌ Falló la publicación. Abortando build." $Red
            return
        }
    }
    
    # Verificar build
    Invoke-Verify
    
    # Mostrar estadísticas
    Show-Statistics
    
    Write-ColorOutput ""
    Write-ColorOutput "=" * 60 $White
    
    if ($success) {
        Write-ColorOutput "🎉 ¡Build completado exitosamente!" $Green
        Write-ColorOutput "📁 Paquetes disponibles en: $OutputPath" $Cyan
    } else {
        Write-ColorOutput "❌ Build falló. Revisa los errores anteriores." $Red
    }
    
    $endTime = Get-Date
    $totalDuration = $endTime - $startTime
    Write-ColorOutput "⏱️ Tiempo total: $($totalDuration.TotalSeconds.ToString('F2')) segundos" $White
}

# Ejecutar función principal
Main 