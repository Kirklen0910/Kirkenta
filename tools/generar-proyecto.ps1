# ============================================================
# GENERADOR DE SNAPSHOTS - KIRKENTA ERP
# Ruta: tools\generar-proyecto.ps1
# ============================================================
# Uso (desde cualquier carpeta):
#   powershell -ExecutionPolicy Bypass -File tools\generar-proyecto.ps1
#
# Resultado: carpeta "snapshots" en la raiz del proyecto con los
# archivos PROYECTO_PARTE_*.md. ESOS son los UNICOS archivos que
# hay que subir al chat (nada mas).
#
# Que NO incluye (a proposito):
#   - Migrations (son codigo generado, ~800 KB de ruido)
#   - *.sql y *.txt (backups, scripts de reset, notas)
#   - bin, obj, .git, uploads, backups, wwwroot/lib, imagenes
#   - Contrasenas: en appsettings*.json se reemplazan por ***
#
# NOTA: este archivo usa solo caracteres ASCII a proposito, para
# que Windows PowerShell 5.1 lo lea bien sin importar la codificacion.
# ============================================================

$ErrorActionPreference = "Stop"

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path.TrimEnd('\')
$carpetaSalida = Join-Path $raiz "snapshots"
$maxCaracteresPorArchivo = 1500000

# ------------------------------------------------------------
# Filtros
# ------------------------------------------------------------
$carpetasExcluidas = @(
    "bin", "obj", ".git", ".vs", ".vscode", ".idea", "node_modules",
    "lib", "images", "Migrations", "uploads", "backups", "snapshots",
    "TestResults", "packages"
)

$extensionesIncluidas = @(".cs", ".cshtml", ".json", ".csproj", ".sln", ".css", ".js", ".md")
$nombresIncluidos = @(".gitignore")

$patronesExcluidos = @(
    "*.Designer.cs", "*.AssemblyInfo.cs", "*.g.cs",
    "*.min.js", "*.min.css", "package-lock.json",
    "PROYECTO*.md", "*.log", "*.bak", "*.tmp"
)

# ------------------------------------------------------------
# Partes del snapshot
# ------------------------------------------------------------
$partes = @(
    @{ Numero = 1; Nombre = "Nucleo";      Descripcion = "Data, Models, Helpers base, Usuarios, Auth, Configuracion, Series, Services, wwwroot, Program.cs" },
    @{ Numero = 2; Nombre = "Operaciones"; Descripcion = "POS, Ventas, Compras, Inventario, Clientes, Proveedores, Finanzas (paginas y helpers)" },
    @{ Numero = 3; Nombre = "Soporte";     Descripcion = "RRHH, Logistica, Reportes (paginas y helpers)" },
    @{ Numero = 4; Nombre = "Extras";      Descripcion = "Modulos nuevos auto-descubiertos que no encajan en las partes anteriores" }
)

function Get-NumeroParte {
    param([string]$rel)

    if ($rel -match '^Pages/(RRHH|Logistica|Reportes)/') { return 3 }
    if ($rel -match '^Helpers/(RRHH|Logistica)/') { return 3 }

    if ($rel -match '^Pages/(Bajas|Categorias|Clientes|Compras|Cotizaciones|Devoluciones|Facturas|Finanzas|OrdenesCompra|Pedidos|PagosProveedor|POS|Productos|Proveedores|Ventas)/') { return 2 }
    if ($rel -match '^Helpers/Finanzas/') { return 2 }

    if ($rel -notmatch '/') { return 1 }
    if ($rel -match '^(Data|Models|Services|Properties|wwwroot)/') { return 1 }
    if ($rel -match '^Helpers/[^/]+$') { return 1 }
    if ($rel -match '^Helpers/(Export|Import)/') { return 1 }
    if ($rel -match '^Pages/(Auth|Usuarios|Configuracion|Impuestos|MetodosPago|Nomenclatura|Series|UnidadesMedida|Shared)/') { return 1 }
    if ($rel -match '^Pages/[^/]+$') { return 1 }

    return 4
}

function Get-Lenguaje {
    param([string]$ext)
    switch ($ext) {
        ".cs"     { return "csharp" }
        ".cshtml" { return "html" }
        ".json"   { return "json" }
        ".csproj" { return "xml" }
        ".css"    { return "css" }
        ".js"     { return "javascript" }
        ".md"     { return "markdown" }
        default   { return "" }
    }
}

function Ocultar-Secretos {
    param([string]$texto)

    # Password=...; / Pwd=...; (cadenas de conexion)
    $texto = [regex]::Replace($texto, '(?i)((?:password|pwd)\s*=\s*)(''[^'']*''|[^;"\r\n]*)', '${1}***')

    # "ClaveSecreta": "valor" (claves JSON con nombres sensibles)
    $texto = [regex]::Replace($texto, '(?i)("[^"]*(?:secret|token|apikey|password|clave)[^"]*"\s*:\s*")[^"]*(")', '${1}***${2}')

    return $texto
}

function Get-Bloque {
    param($archivo)

    $texto = ""
    try {
        $texto = [System.IO.File]::ReadAllText($archivo.Ruta, [System.Text.Encoding]::UTF8)
    }
    catch {
        $texto = "// ERROR al leer el archivo: " + $_.Exception.Message
    }

    $texto = $texto.Replace("`r`n", "`n")

    if ($archivo.Nombre -like "appsettings*.json") {
        $texto = Ocultar-Secretos $texto
    }

    $lang = Get-Lenguaje $archivo.Ext

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("===== FILE: " + $archivo.Rel + " =====")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('````' + $lang)
    [void]$sb.AppendLine($texto.TrimEnd())
    [void]$sb.AppendLine('````')
    [void]$sb.AppendLine()
    return $sb.ToString()
}

# ------------------------------------------------------------
# Recolectar archivos
# ------------------------------------------------------------
Write-Host ""
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " GENERADOR DE SNAPSHOTS - KIRKENTA ERP" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Raiz:   $raiz" -ForegroundColor Yellow
Write-Host "Salida: $carpetaSalida" -ForegroundColor Yellow
Write-Host ""
Write-Host "Buscando archivos..." -ForegroundColor Yellow

$archivos = New-Object System.Collections.Generic.List[object]

foreach ($f in (Get-ChildItem -Path $raiz -Recurse -File)) {
    $rel = $f.FullName.Substring($raiz.Length + 1).Replace('\', '/')
    $segmentos = $rel.Split('/')

    $enCarpetaExcluida = $false
    for ($i = 0; $i -lt ($segmentos.Length - 1); $i++) {
        if ($carpetasExcluidas -contains $segmentos[$i]) { $enCarpetaExcluida = $true; break }
    }
    if ($enCarpetaExcluida) { continue }

    $nombreExcluido = $false
    foreach ($patron in $patronesExcluidos) {
        if ($f.Name -like $patron) { $nombreExcluido = $true; break }
    }
    if ($nombreExcluido) { continue }

    $ext = $f.Extension.ToLower()
    if (($extensionesIncluidas -notcontains $ext) -and ($nombresIncluidos -notcontains $f.Name)) { continue }

    $archivos.Add([pscustomobject]@{
        Rel    = $rel
        Ruta   = $f.FullName
        Nombre = $f.Name
        Ext    = $ext
        Parte  = (Get-NumeroParte $rel)
    })
}

Write-Host "Archivos encontrados: $($archivos.Count)" -ForegroundColor Green
Write-Host ""

# ------------------------------------------------------------
# Preparar carpeta de salida (borra partes viejas)
# ------------------------------------------------------------
if (-not (Test-Path $carpetaSalida)) {
    New-Item -ItemType Directory -Path $carpetaSalida | Out-Null
}
Get-ChildItem -Path $carpetaSalida -Filter "PROYECTO_PARTE_*.md" -File | Remove-Item -Force

$utf8SinBom = New-Object System.Text.UTF8Encoding($false)
$fecha = Get-Date -Format "dd/MM/yyyy HH:mm"
$nombreProyecto = Split-Path $raiz -Leaf
$totalBytes = 0
$totalArchivosEscritos = 0

# ------------------------------------------------------------
# Generar cada parte
# ------------------------------------------------------------
foreach ($p in $partes) {
    $lista = @($archivos | Where-Object { $_.Parte -eq $p.Numero } | Sort-Object Rel)
    if ($lista.Count -eq 0) { continue }

    # Dividir en tandas si la parte supera el maximo de caracteres
    $tandas = New-Object System.Collections.Generic.List[object]
    $actual = @{
        Bloques = (New-Object System.Collections.Generic.List[string])
        Rels    = (New-Object System.Collections.Generic.List[string])
        Largo   = 0
    }

    foreach ($a in $lista) {
        $bloque = Get-Bloque $a

        if (($actual.Largo -gt 0) -and (($actual.Largo + $bloque.Length) -gt $maxCaracteresPorArchivo)) {
            $tandas.Add($actual)
            $actual = @{
                Bloques = (New-Object System.Collections.Generic.List[string])
                Rels    = (New-Object System.Collections.Generic.List[string])
                Largo   = 0
            }
        }

        $actual.Bloques.Add($bloque)
        $actual.Rels.Add($a.Rel)
        $actual.Largo += $bloque.Length
    }
    if ($actual.Bloques.Count -gt 0) { $tandas.Add($actual) }

    for ($k = 0; $k -lt $tandas.Count; $k++) {
        $tanda = $tandas[$k]

        $sufijo = ""
        if ($tandas.Count -gt 1) { $sufijo = "_" + ($k + 1) }
        $nombreArchivo = "PROYECTO_PARTE_{0}_{1}{2}.md" -f $p.Numero, $p.Nombre, $sufijo

        $sb = New-Object System.Text.StringBuilder
        [void]$sb.AppendLine("# KIRKENTA ERP - SNAPSHOT: PARTE $($p.Numero): $($p.Nombre.ToUpper())")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("**Fecha de generacion:** $fecha")
        [void]$sb.AppendLine("**Proyecto:** $nombreProyecto")
        [void]$sb.AppendLine("**Descripcion:** $($p.Descripcion)")
        if ($tandas.Count -gt 1) {
            [void]$sb.AppendLine("**Archivo:** $($k + 1) de $($tandas.Count) de esta parte")
        }
        [void]$sb.AppendLine("**Archivos de codigo en este archivo:** $($tanda.Rels.Count)")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("---")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("## INDICE")
        [void]$sb.AppendLine()
        foreach ($r in $tanda.Rels) {
            [void]$sb.AppendLine("- $r")
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("---")
        [void]$sb.AppendLine()

        foreach ($b in $tanda.Bloques) {
            [void]$sb.Append($b)
        }

        $rutaSalida = Join-Path $carpetaSalida $nombreArchivo
        [System.IO.File]::WriteAllText($rutaSalida, $sb.ToString(), $utf8SinBom)

        $info = Get-Item $rutaSalida
        $totalBytes += $info.Length
        $totalArchivosEscritos += $tanda.Rels.Count

        Write-Host ("  {0,-42} {1,7} KB  {2,4} archivos" -f $nombreArchivo, [math]::Round($info.Length / 1KB, 0), $tanda.Rels.Count)
    }
}

Write-Host ""
Write-Host "====================================================" -ForegroundColor Green
Write-Host " SNAPSHOTS GENERADOS" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host "Carpeta: $carpetaSalida"
Write-Host ("Archivos de codigo incluidos: {0}" -f $totalArchivosEscritos)
Write-Host ("Tamano total: {0} KB ({1} MB)" -f [math]::Round($totalBytes / 1KB, 0), [math]::Round($totalBytes / 1MB, 2))
Write-Host ""
Write-Host "Sube SOLO los archivos PROYECTO_PARTE_*.md de esa carpeta." -ForegroundColor Yellow
Write-Host ""
