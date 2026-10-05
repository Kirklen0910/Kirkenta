# ============================================
# GENERADOR DE SNAPSHOT PARA KIRKENTA ERP
# Genera PROYECTO.md con TODO el codigo fuente
# ============================================

$raiz = $PSScriptRoot
$output = Join-Path $raiz "PROYECTO.md"

# Extensiones a incluir
$extensiones = @("*.cs", "*.cshtml", "*.csproj", "*.json", "*.css", "*.js", "*.md", "*.sql")
# Extensiones a EXCLUIR (archivos generados)
$excluir = @("*.Designer.cs", "*.AssemblyInfo.cs", "*.g.cs")

# Carpetas a EXCLUIR
$carpetasExcluir = @(
    "bin", "obj", ".vs", ".vscode", ".idea",
    "node_modules", ".git",
    "uploads"
)

function DebeExcluirCarpeta {
    param($ruta)
    foreach ($c in $carpetasExcluir) {
        if ($ruta -match "\\$c\\" -or $ruta -match "\\$c$") { return $true }
    }
    return $false
}

function DebeExcluirArchivo {
    param($nombre)
    foreach ($e in $excluir) {
        if ($nombre -like $e) { return $true }
    }
    return $false
}

# Iniciar el archivo
$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("# KIRKENTA ERP - SNAPSHOT COMPLETO DEL PROYECTO")
[void]$sb.AppendLine()
[void]$sb.AppendLine("**Fecha de generacion:** $(Get-Date -Format 'dd/MM/yyyy HH:mm')")
[void]$sb.AppendLine("**Raiz del proyecto:** $raiz")
[void]$sb.AppendLine()
[void]$sb.AppendLine("---")
[void]$sb.AppendLine()

# Contar archivos primero
$todosLosArchivos = @()
foreach ($ext in $extensiones) {
    $encontrados = Get-ChildItem -Path $raiz -Filter $ext -Recurse -File |
        Where-Object {
            -not (DebeExcluirCarpeta $_.FullName) -and
            -not (DebeExcluirArchivo $_.Name)
        }
    $todosLosArchivos += $encontrados
}

$todosLosArchivos = $todosLosArchivos | Sort-Object FullName -Unique
$total = $todosLosArchivos.Count

[void]$sb.AppendLine("**Total archivos:** $total")
[void]$sb.AppendLine()

# Agrupar por carpeta
$agrupados = $todosLosArchivos | Group-Object { 
    $rel = $_.FullName.Replace("$raiz\", "")
    $dir = Split-Path $rel -Parent
    if ([string]::IsNullOrEmpty($dir)) { return "RAIZ" }
    return $dir
} | Sort-Object Name

foreach ($grupo in $agrupados) {
    $nombreGrupo = $grupo.Name
    $archivosGrupo = $grupo.Group | Sort-Object Name
    
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine(" $nombreGrupo - $($archivosGrupo.Count) archivo(s)")
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine()

    foreach ($archivo in $archivosGrupo) {
        $rel = $archivo.FullName.Replace("$raiz\", "").Replace("\", "/")
        
        [void]$sb.AppendLine("===== FILE: $rel =====")
        [void]$sb.AppendLine()
        
        # Detectar extension para el bloque de codigo
        $ext = $archivo.Extension.TrimStart('.').ToLower()
        $lenguaje = switch ($ext) {
            "cs" { "csharp" }
            "cshtml" { "html" }
            "csproj" { "xml" }
            "json" { "json" }
            "css" { "css" }
            "js" { "javascript" }
            "md" { "markdown" }
            "sql" { "sql" }
            default { "" }
        }
        
        [void]$sb.AppendLine("````$lenguaje")
        
        try {
            $contenido = Get-Content -Path $archivo.FullName -Raw -Encoding UTF8
            if ($null -eq $contenido) { $contenido = "" }
            [void]$sb.AppendLine($contenido)
        } catch {
            [void]$sb.AppendLine("// ERROR AL LEER: $($_.Exception.Message)")
        }
        
        [void]$sb.AppendLine("````")
        [void]$sb.AppendLine()
    }
}

# Escribir el archivo
$sb.ToString() | Out-File -FilePath $output -Encoding UTF8

$tamanoMB = [math]::Round((Get-Item $output).Length / 1MB, 2)
Write-Host "OK - Snapshot generado: $output" -ForegroundColor Green
Write-Host "Total archivos: $total" -ForegroundColor Cyan
Write-Host "Tamano: $tamanoMB MB" -ForegroundColor Cyan