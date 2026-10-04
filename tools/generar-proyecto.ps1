# ============================================================
# GENERADOR DE PROYECTO.md — Snapshot completo de código
# ============================================================
# Uso: powershell -ExecutionPolicy Bypass -File tools\generar-proyecto.ps1
# ============================================================

$ErrorActionPreference = "Stop"

# Función para detectar lenguaje por extensión
function Get-Lang {
    param([string]$ext)
    switch ($ext.ToLower()) {
        ".cs"      { return "csharp" }
        ".cshtml"  { return "html" }
        ".json"    { return "json" }
        ".xml"     { return "xml" }
        ".csproj"  { return "xml" }
        ".sln"     { return "" }
        ".css"     { return "css" }
        ".js"      { return "javascript" }
        ".sql"     { return "sql" }
        ".md"      { return "markdown" }
        default    { return "" }
    }
}

$raiz = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$salida = Join-Path $raiz "PROYECTO.md"

Write-Host ""
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " GENERADOR DE PROYECTO.md - KIRKENTA ERP" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Raiz: $raiz" -ForegroundColor Yellow
Write-Host "Salida: $salida" -ForegroundColor Yellow
Write-Host ""

# Carpetas a excluir
$excluirCarpetas = @(
    "bin", "obj", ".git", ".vs", ".vscode", "node_modules",
    "lib", "images", "Migrations", "uploads"
)

# Patrones de archivos a excluir
$excluirArchivos = @(
    "*.dll", "*.pdb", "*.exe", "*.user", "*.suo", "*.cache",
    "*.png", "*.jpg", "*.jpeg", "*.gif", "*.ico", "*.svg", "*.webp",
    "*.zip", "*.rar", "*.7z", "*.pdf", "*.xlsx", "*.docx",
    "PROYECTO.md", "CONTEXTO.md", "*.tmp", "*.log", "*.bak",
    "*.min.js", "*.min.css"
)

# Archivos específicos que SÍ queremos incluir
$incluirEspecificos = @(".gitignore")

# Extensiones a incluir
$incluirExtensiones = @(
    "*.cs", "*.cshtml", "*.json", "*.csproj", "*.sln",
    "*.css", "*.js", "*.sql", "*.md"
)

# ============================================================
# Recolectar archivos
# ============================================================
Write-Host "Buscando archivos..." -ForegroundColor Yellow

$archivos = Get-ChildItem -Path $raiz -Recurse -File | Where-Object {
    $archivo = $_
    $ruta = $archivo.FullName

    $enCarpetaExcluida = $false
    foreach ($carpeta in $excluirCarpetas) {
        if ($ruta -match "\\$carpeta\\") { $enCarpetaExcluida = $true; break }
    }
    if ($enCarpetaExcluida) { return $false }

    foreach ($patron in $excluirArchivos) {
        if ($archivo.Name -like $patron) { return $false }
    }

    if ($incluirEspecificos -contains $archivo.Name) { return $true }

    foreach ($ext in $incluirExtensiones) {
        if ($archivo.Name -like $ext) { return $true }
    }

    return $false
}

Write-Host "Encontrados $($archivos.Count) archivos" -ForegroundColor Green
Write-Host ""

# ============================================================
# Agrupar por categoría
# ============================================================
$raizNorm = $raiz.TrimEnd('\')

$categorias = @(
    @{ Nombre = "RAIZ - Program, csproj, appsettings, gitignore"; Filtro = { $_.DirectoryName.TrimEnd('\') -eq $raizNorm } },
    @{ Nombre = "DATA"; Filtro = { $_.FullName -match "\\Data\\" } },
    @{ Nombre = "MODELS"; Filtro = { $_.FullName -match "\\Models\\" } },
    @{ Nombre = "HELPERS - RAIZ"; Filtro = { $_.DirectoryName.TrimEnd('\') -eq (Join-Path $raizNorm "Helpers") } },
    @{ Nombre = "HELPERS - EXPORT"; Filtro = { $_.FullName -match "\\Helpers\\Export\\" } },
    @{ Nombre = "HELPERS - IMPORT"; Filtro = { $_.FullName -match "\\Helpers\\Import\\" } },
    @{ Nombre = "HELPERS - FINANZAS"; Filtro = { $_.FullName -match "\\Helpers\\Finanzas\\" } },
    @{ Nombre = "HELPERS - RRHH"; Filtro = { $_.FullName -match "\\Helpers\\RRHH\\" } },
    @{ Nombre = "PAGES - AUTH"; Filtro = { $_.FullName -match "\\Pages\\Auth\\" } },
    @{ Nombre = "PAGES - USUARIOS"; Filtro = { $_.FullName -match "\\Pages\\Usuarios\\" } },
    @{ Nombre = "PAGES - CLIENTES"; Filtro = { $_.FullName -match "\\Pages\\Clientes\\" } },
    @{ Nombre = "PAGES - PRODUCTOS / CATEGORIAS / UNIDADES"; Filtro = { $_.FullName -match "\\Pages\\(Productos|Categorias|UnidadesMedida)\\" } },
    @{ Nombre = "PAGES - POS / VENTAS / COTIZ / PEDIDOS / FACTURAS / DEVOL"; Filtro = { $_.FullName -match "\\Pages\\(POS|Ventas|Cotizaciones|Pedidos|Facturas|Devoluciones)\\" } },
    @{ Nombre = "PAGES - BAJAS"; Filtro = { $_.FullName -match "\\Pages\\Bajas\\" } },
    @{ Nombre = "PAGES - COMPRAS / PROVEEDORES / ORDENES / PAGOS"; Filtro = { $_.FullName -match "\\Pages\\(Compras|Proveedores|OrdenesCompra|PagosProveedor)\\" } },
    @{ Nombre = "PAGES - FINANZAS base"; Filtro = { $_.FullName -match "\\Pages\\Finanzas\\" -and $_.FullName -notmatch "\\Pages\\Finanzas\\(PlanCuentas|Contabilidad|CierresContables|Conciliacion)\\" } },
    @{ Nombre = "PAGES - FINANZAS - PLAN DE CUENTAS"; Filtro = { $_.FullName -match "\\Pages\\Finanzas\\PlanCuentas\\" } },
    @{ Nombre = "PAGES - FINANZAS - CONTABILIDAD"; Filtro = { $_.FullName -match "\\Pages\\Finanzas\\Contabilidad\\" } },
    @{ Nombre = "PAGES - FINANZAS - CIERRES CONTABLES"; Filtro = { $_.FullName -match "\\Pages\\Finanzas\\CierresContables\\" } },
    @{ Nombre = "PAGES - FINANZAS - CONCILIACION"; Filtro = { $_.FullName -match "\\Pages\\Finanzas\\Conciliacion\\" } },
    @{ Nombre = "PAGES - RRHH"; Filtro = { $_.FullName -match "\\Pages\\RRHH\\" } },
    @{ Nombre = "PAGES - REPORTES"; Filtro = { $_.FullName -match "\\Pages\\Reportes\\" } },
    @{ Nombre = "PAGES - CONFIG / IMPUESTOS / METODOS / NOMENC / SERIES"; Filtro = { $_.FullName -match "\\Pages\\(Configuracion|Impuestos|MetodosPago|Nomenclatura|Series)\\" } },
    @{ Nombre = "PAGES - SHARED"; Filtro = { $_.FullName -match "\\Pages\\Shared\\" } },
    @{ Nombre = "PAGES - RAIZ"; Filtro = { $_.DirectoryName.TrimEnd('\') -eq (Join-Path $raizNorm "Pages") } },
    @{ Nombre = "WWWROOT - CSS / JS"; Filtro = { $_.FullName -match "\\wwwroot\\(css|js)\\" } },
    @{ Nombre = "PROPERTIES"; Filtro = { $_.FullName -match "\\Properties\\" } }
)

# ============================================================
# Construir el archivo
# ============================================================
Write-Host "Construyendo PROYECTO.md..." -ForegroundColor Yellow

$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine("# KIRKENTA ERP - SNAPSHOT COMPLETO DEL PROYECTO")
[void]$sb.AppendLine()
[void]$sb.AppendLine("**Fecha de generacion:** $(Get-Date -Format 'dd/MM/yyyy HH:mm')")
[void]$sb.AppendLine("**Raiz del proyecto:** $raiz")
[void]$sb.AppendLine("**Total archivos:** $($archivos.Count)")
[void]$sb.AppendLine("**Uso:** Pegar completo al inicio de un chat para dar contexto total del codigo fuente.")
[void]$sb.AppendLine()
[void]$sb.AppendLine("---")
[void]$sb.AppendLine()

$yaIncluidos = @{}

foreach ($cat in $categorias) {
    $archivosCat = $archivos | Where-Object -FilterScript $cat.Filtro | Sort-Object FullName
    if ($archivosCat.Count -eq 0) { continue }

    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine(" $($cat.Nombre) - $($archivosCat.Count) archivo(s)")
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine("")

    foreach ($archivo in $archivosCat) {
        if ($yaIncluidos.ContainsKey($archivo.FullName)) { continue }
        $yaIncluidos[$archivo.FullName] = $true

        $rutaRel = $archivo.FullName.Substring($raiz.Length + 1).Replace("\", "/")
        $lang = Get-Lang $archivo.Extension

        [void]$sb.AppendLine("===== FILE: $rutaRel =====")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine('```' + $lang)
        try {
            $texto = Get-Content -Path $archivo.FullName -Raw -Encoding UTF8
            [void]$sb.AppendLine($texto)
        } catch {
            [void]$sb.AppendLine("// ERROR al leer el archivo: $_")
        }
        [void]$sb.AppendLine('```')
        [void]$sb.AppendLine()
    }
}

# Guardar con UTF-8 sin BOM
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($salida, $sb.ToString(), $utf8SinBom)

$info = Get-Item $salida
$tamKB = [math]::Round($info.Length / 1KB, 0)
$tamMB = [math]::Round($info.Length / 1MB, 2)

Write-Host ""
Write-Host "====================================================" -ForegroundColor Green
Write-Host " SNAPSHOT GENERADO" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host "Archivo: $salida"
Write-Host "Archivos incluidos: $($yaIncluidos.Count) de $($archivos.Count)"
Write-Host ("Tamano: " + $tamKB + " KB " + $tamMB + " MB")
Write-Host ""