<#
.SYNOPSIS
    Genera 4 snapshots del proyecto Kirkenta ERP con auto-descubrimiento de modulos.
.DESCRIPTION
    Escanea el proyecto y reparte TODO el codigo en 4 partes:
      - PARTE 1: Nucleo (Data, Models, Helpers base, Usuarios, Config, wwwroot)
      - PARTE 2: Operaciones (Ventas, POS, Compras, Finanzas, Inventario)
      - PARTE 3: Soporte (Logistica, RRHH, Reportes)
      - PARTE 4: Extras (cualquier modulo nuevo: Produccion, Activos, Presupuestos, etc.)

    NO HAY QUE EDITAR ESTE SCRIPT cuando se agregan modulos nuevos.
    Todo se detecta automaticamente.
.EXAMPLE
    .\generar-snapshot.ps1
#>

param(
    [string]$RaizProyecto = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

# ============================================================
# CONFIGURACION
# ============================================================

$FechaGeneracion = Get-Date -Format "dd/MM/yyyy HH:mm"

$ExtensionesIncluidas = @(
    '.cs', '.cshtml', '.razor',
    '.json', '.config',
    '.css', '.scss', '.sass', '.less',
    '.js', '.jsx', '.ts', '.tsx',
    '.md', '.txt',
    '.sql',
    '.csproj', '.sln', '.props', '.targets',
    '.ps1', '.psm1', '.psd1',
    '.xml', '.yml', '.yaml',
    '.html', '.htm',
    '.svg',
    '.bat', '.cmd', '.sh'
)

$ExcluirSiempre = @(
    'bin', 'obj', '.vs', '.vscode', 'node_modules', '.git',
    'uploads', 'lib'
)

$ArchivosExcluidos = @(
    'PROYECTO.md',
    'PROYECTO_PARTE_1_Nucleo.md',
    'PROYECTO_PARTE_2_Operaciones.md',
    'PROYECTO_PARTE_3_Soporte.md',
    'PROYECTO_PARTE_4_Extras.md',
    'generar-snapshot.ps1',
    'appsettings.Development.json'
)

# ============================================================
# CLASIFICACION DE MODULOS POR PARTE
# ============================================================
# Si aparece una carpeta en Pages/ o Helpers/ que NO este en ninguna
# de estas listas, ira automaticamente a la PARTE 4.

# --- PARTE 1: Nucleo ---
$Parte1_PagesCarpetas = @(
    'Auth', 'Usuarios', 'Configuracion', 'Nomenclatura',
    'Series', 'MetodosPago', 'Impuestos', 'UnidadesMedida'
)

# --- PARTE 2: Operaciones ---
$Parte2_PagesCarpetas = @(
    'POS', 'Ventas', 'Clientes', 'Cotizaciones', 'Pedidos',
    'Facturas', 'Devoluciones', 'Productos', 'Categorias',
    'Bajas', 'Compras', 'Proveedores', 'OrdenesCompra',
    'PagosProveedor', 'Finanzas'
)
$Parte2_HelpersCarpetas = @('Finanzas')

# --- PARTE 3: Soporte ---
$Parte3_PagesCarpetas = @(
    'Logistica', 'RRHH', 'Reportes'
)
$Parte3_HelpersCarpetas = @('Logistica', 'RRHH')

# Cualquier carpeta NO listada -> va a PARTE 4 automaticamente

# ============================================================
# FUNCIONES AUXILIARES
# ============================================================

function Test-Excluido {
    param([string]$RutaRelativa)

    foreach ($excl in $ExcluirSiempre) {
        if ($RutaRelativa -like "*\$excl\*" -or $RutaRelativa -like "$excl\*" -or $RutaRelativa -like "*\$excl" -or $RutaRelativa -eq $excl) {
            return $true
        }
    }
    return $false
}

function Get-LenguajeBloque {
    param([string]$Extension)

    switch ($Extension.ToLower()) {
        '.cs'      { return 'csharp' }
        '.cshtml'  { return 'html' }
        '.razor'   { return 'html' }
        '.json'    { return 'json' }
        '.config'  { return 'xml' }
        '.css'     { return 'css' }
        '.scss'    { return 'scss' }
        '.sass'    { return 'sass' }
        '.less'    { return 'less' }
        '.js'      { return 'javascript' }
        '.jsx'     { return 'jsx' }
        '.ts'      { return 'typescript' }
        '.tsx'     { return 'tsx' }
        '.md'      { return 'markdown' }
        '.txt'     { return 'plaintext' }
        '.sql'     { return 'sql' }
        '.xml'     { return 'xml' }
        '.yml'     { return 'yaml' }
        '.yaml'    { return 'yaml' }
        '.csproj'  { return 'xml' }
        '.sln'     { return 'plaintext' }
        '.props'   { return 'xml' }
        '.targets' { return 'xml' }
        '.ps1'     { return 'powershell' }
        '.psm1'    { return 'powershell' }
        '.psd1'    { return 'powershell' }
        '.html'    { return 'html' }
        '.htm'     { return 'html' }
        '.svg'     { return 'xml' }
        '.bat'     { return 'batch' }
        '.cmd'     { return 'batch' }
        '.sh'      { return 'bash' }
        default    { return 'plaintext' }
    }
}

function Get-TodosLosArchivos {
    param([string]$Raiz)

    $extensionesRegex = ($ExtensionesIncluidas | ForEach-Object {
        [regex]::Escape($_)
    }) -join '|'

    $todos = Get-ChildItem -Path $Raiz -Recurse -File -Force |
        Where-Object {
            $_.Extension -match "^($extensionesRegex)$" -or
            $_.Name -eq '.gitignore' -or
            $_.Name -eq '.gitattributes' -or
            $_.Name -eq '.editorconfig'
        }

    $resultado = @()
    foreach ($f in $todos) {
        $rutaRel = $f.FullName.Substring($Raiz.Length).TrimStart('\')
        if (Test-Excluido $rutaRel) { continue }
        if ($ArchivosExcluidos -contains $f.Name) { continue }
        $resultado += $f
    }

    return $resultado | Sort-Object FullName -Unique
}

function Clasificar-Archivo {
    param(
        [System.IO.FileInfo]$Archivo,
        [string]$Raiz
    )

    $rutaRel = $Archivo.FullName.Substring($Raiz.Length).TrimStart('\')
    $partes = $rutaRel -split '\\'

    # --- RAIZ (archivos sueltos como Program.cs, .sln, appsettings.json) ---
    if ($partes.Count -eq 1) {
        return 'Parte1'
    }

    $primerNivel = $partes[0]

    # --- Data, Models, Migrations, Properties -> Parte 1 ---
    if ($primerNivel -in @('Data', 'Models', 'Migrations', 'Properties')) {
        return 'Parte1'
    }

    # --- wwwroot -> Parte 1 (css/js) ---
    if ($primerNivel -eq 'wwwroot') {
        return 'Parte1'
    }

    # --- Helpers -> revisar subcarpeta ---
    if ($primerNivel -eq 'Helpers') {
        if ($partes.Count -eq 2) {
            # Helpers/ActividadHelper.cs, Helpers/ModulosERP.cs, etc.
            return 'Parte1'
        }

        $segundoNivel = $partes[1]

        if ($Parte2_HelpersCarpetas -contains $segundoNivel) { return 'Parte2' }
        if ($Parte3_HelpersCarpetas -contains $segundoNivel) { return 'Parte3' }

        # Helpers/Export, Helpers/Import y cualquier otro -> Parte 1
        return 'Parte1'
    }

    # --- Pages -> revisar subcarpeta ---
    if ($primerNivel -eq 'Pages') {
        if ($partes.Count -eq 2) {
            # Pages/_ViewImports.cshtml, Pages/Index.cshtml, etc.
            return 'Parte1'
        }

        $segundoNivel = $partes[1]

        # Shared siempre va con Nucleo (Parte 1), salvo _LayoutPOS
        if ($segundoNivel -eq 'Shared') {
            if ($partes.Count -ge 3 -and $partes[2] -eq '_LayoutPOS.cshtml') {
                return 'Parte2'
            }
            return 'Parte1'
        }

        if ($Parte1_PagesCarpetas -contains $segundoNivel) { return 'Parte1' }
        if ($Parte2_PagesCarpetas -contains $segundoNivel) { return 'Parte2' }
        if ($Parte3_PagesCarpetas -contains $segundoNivel) { return 'Parte3' }

        # Modulo NUEVO no listado -> Parte 4
        return 'Parte4'
    }

    # --- Cualquier otra cosa (docs, scripts, etc.) -> Parte 4 ---
    return 'Parte4'
}

function AgruparPorCarpeta {
    param(
        [System.IO.FileInfo[]]$Archivos,
        [string]$Raiz
    )

    $porCarpeta = @{}

    foreach ($archivo in $Archivos) {
        $rutaRel = $archivo.FullName.Substring($Raiz.Length).TrimStart('\')
        $partes = $rutaRel -split '\\'

        if ($partes.Count -ge 2) {
            $grupo = $partes[0]
            if ($partes.Count -ge 3 -and $partes[0] -eq 'Helpers') {
                $grupo = "$($partes[0])\$($partes[1])"
            } elseif ($partes.Count -ge 3 -and $partes[0] -eq 'Pages') {
                $grupo = "$($partes[0])\$($partes[1])"
            } elseif ($partes.Count -ge 3 -and $partes[0] -eq 'wwwroot') {
                $grupo = "$($partes[0])\$($partes[1])"
            }
        } else {
            $grupo = 'RAIZ'
        }

        if (-not $porCarpeta.ContainsKey($grupo)) {
            $porCarpeta[$grupo] = @()
        }
        $porCarpeta[$grupo] += $archivo
    }

    return $porCarpeta
}

function Escribir-Archivo {
    param(
        [string]$RutaSalida,
        [string]$Titulo,
        [string]$Descripcion,
        [System.IO.FileInfo[]]$Archivos,
        [string]$Raiz,
        [bool]$IncluirIndice = $true
    )

    $sb = New-Object System.Text.StringBuilder

    # Header
    [void]$sb.AppendLine("# KIRKENTA ERP - SNAPSHOT: $Titulo")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("**Fecha de generacion:** $FechaGeneracion")
    [void]$sb.AppendLine("**Raiz del proyecto:** $Raiz")
    [void]$sb.AppendLine("**Descripcion:** $Descripcion")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine()

    $totalArchivos = $Archivos.Count
    [void]$sb.AppendLine("**Total archivos en esta parte:** $totalArchivos")
    [void]$sb.AppendLine()

    if ($totalArchivos -eq 0) {
        [void]$sb.AppendLine("_(No hay archivos en esta parte todavia. Se llenara cuando desarrolles modulos nuevos.)_")
        [void]$sb.AppendLine()
        $utf8WithBom = New-Object System.Text.UTF8Encoding $true
        [System.IO.File]::WriteAllText($RutaSalida, $sb.ToString(), $utf8WithBom)
        return
    }

    $porCarpeta = AgruparPorCarpeta -Archivos $Archivos -Raiz $Raiz

    # Indice
    if ($IncluirIndice) {
        [void]$sb.AppendLine("## INDICE")
        [void]$sb.AppendLine()
        foreach ($grupo in ($porCarpeta.Keys | Sort-Object)) {
            $count = $porCarpeta[$grupo].Count
            [void]$sb.AppendLine("- **$grupo** ($count archivo(s))")
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("---")
        [void]$sb.AppendLine()
    }

    # Contenido agrupado
    foreach ($grupo in ($porCarpeta.Keys | Sort-Object)) {
        [void]$sb.AppendLine("====================================================")
        [void]$sb.AppendLine(" $grupo - $($porCarpeta[$grupo].Count) archivo(s)")
        [void]$sb.AppendLine("====================================================")
        [void]$sb.AppendLine()

        foreach ($archivo in ($porCarpeta[$grupo] | Sort-Object FullName)) {
            $rutaRel = $archivo.FullName.Substring($Raiz.Length).TrimStart('\').Replace('\', '/')

            [void]$sb.AppendLine("===== FILE: $rutaRel =====")
            [void]$sb.AppendLine()

            $lenguaje = Get-LenguajeBloque $archivo.Extension
            $fence = '```' + '`' + $lenguaje
            [void]$sb.AppendLine($fence)

            try {
                $contenido = Get-Content -Path $archivo.FullName -Raw -Encoding UTF8 -ErrorAction Stop
                if ($null -eq $contenido) { $contenido = "" }
                [void]$sb.AppendLine($contenido)
            } catch {
                try {
                    $bytes = [System.IO.File]::ReadAllBytes($archivo.FullName)
                    [void]$sb.AppendLine("// [ARCHIVO BINARIO - $($bytes.Length) bytes]")
                } catch {
                    [void]$sb.AppendLine("// ERROR AL LEER EL ARCHIVO: $($_.Exception.Message)")
                }
            }

            [void]$sb.AppendLine('```' + '`')
            [void]$sb.AppendLine()
        }
    }

    $utf8WithBom = New-Object System.Text.UTF8Encoding $true
    [System.IO.File]::WriteAllText($RutaSalida, $sb.ToString(), $utf8WithBom)
}

# ============================================================
# EJECUCION
# ============================================================

Write-Host ""
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "   KIRKENTA ERP - GENERADOR DE SNAPSHOTS (x4)" -ForegroundColor Cyan
Write-Host "   Auto-descubrimiento de modulos activado" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Raiz: $RaizProyecto" -ForegroundColor Gray
Write-Host ""

# 1. Obtener TODOS los archivos
Write-Host "[*] Escaneando proyecto..." -ForegroundColor Cyan
$todosArchivos = Get-TodosLosArchivos -Raiz $RaizProyecto
Write-Host "    Total de archivos encontrados: $($todosArchivos.Count)" -ForegroundColor Green
Write-Host ""

# 2. Clasificar cada archivo
Write-Host "[*] Clasificando archivos por parte..." -ForegroundColor Cyan

$buckets = @{
    'Parte1' = @()
    'Parte2' = @()
    'Parte3' = @()
    'Parte4' = @()
}

foreach ($archivo in $todosArchivos) {
    $clasificacion = Clasificar-Archivo -Archivo $archivo -Raiz $RaizProyecto
    $buckets[$clasificacion] += $archivo
}

Write-Host "    Parte 1 (Nucleo):      $($buckets['Parte1'].Count) archivo(s)" -ForegroundColor Green
Write-Host "    Parte 2 (Operaciones): $($buckets['Parte2'].Count) archivo(s)" -ForegroundColor Green
Write-Host "    Parte 3 (Soporte):     $($buckets['Parte3'].Count) archivo(s)" -ForegroundColor Green
Write-Host "    Parte 4 (Extras):      $($buckets['Parte4'].Count) archivo(s)" -ForegroundColor Green
Write-Host ""

# 3. Detectar modulos NUEVOS (Parte 4)
if ($buckets['Parte4'].Count -gt 0) {
    $modulosNuevos = @{}
    foreach ($archivo in $buckets['Parte4']) {
        $rutaRel = $archivo.FullName.Substring($RaizProyecto.Length).TrimStart('\')
        $partes = $rutaRel -split '\\'
        if ($partes.Count -ge 2) {
            $modulo = "$($partes[0])\$($partes[1])"
            if (-not $modulosNuevos.ContainsKey($modulo)) {
                $modulosNuevos[$modulo] = 0
            }
            $modulosNuevos[$modulo]++
        }
    }

    if ($modulosNuevos.Count -gt 0) {
        Write-Host "    [!] Modulos NUEVOS detectados (van a Parte 4):" -ForegroundColor Yellow
        foreach ($modulo in ($modulosNuevos.Keys | Sort-Object)) {
            Write-Host "        - $modulo ($($modulosNuevos[$modulo]) archivo(s))" -ForegroundColor Yellow
        }
        Write-Host ""
    }
}

# 4. Escribir los 4 archivos
Write-Host "[*] Generando snapshots..." -ForegroundColor Cyan
Write-Host ""

$partesConfig = @(
    @{
        Clave = 'Parte1'
        Nombre = 'PROYECTO_PARTE_1_Nucleo.md'
        Titulo = 'PARTE 1: NUCLEO Y CONFIGURACION'
        Descripcion = 'Data, Models, Migrations, Helpers base, Usuarios, Auth, Configuracion, Series, wwwroot, Program.cs'
    },
    @{
        Clave = 'Parte2'
        Nombre = 'PROYECTO_PARTE_2_Operaciones.md'
        Titulo = 'PARTE 2: OPERACIONES'
        Descripcion = 'POS, Ventas, Compras, Inventario, Finanzas'
    },
    @{
        Clave = 'Parte3'
        Nombre = 'PROYECTO_PARTE_3_Soporte.md'
        Titulo = 'PARTE 3: SOPORTE'
        Descripcion = 'Logistica, RRHH, Reportes'
    },
    @{
        Clave = 'Parte4'
        Nombre = 'PROYECTO_PARTE_4_Extras.md'
        Titulo = 'PARTE 4: EXTRAS / MODULOS NUEVOS'
        Descripcion = 'Modulos nuevos auto-descubiertos (Produccion, Activos, Presupuestos, etc.)'
    }
)

foreach ($parte in $partesConfig) {
    $archivos = $buckets[$parte.Clave]
    $rutaSalida = Join-Path $RaizProyecto $parte.Nombre

    Escribir-Archivo `
        -RutaSalida $rutaSalida `
        -Titulo $parte.Titulo `
        -Descripcion $parte.Descripcion `
        -Archivos $archivos `
        -Raiz $RaizProyecto

    $tamanoKB = [math]::Round((Get-Item $rutaSalida).Length / 1KB, 2)
    Write-Host "    -> $($parte.Nombre) - $tamanoKB KB ($($archivos.Count) archivos)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "====================================================" -ForegroundColor Green
Write-Host "   SNAPSHOTS GENERADOS CORRECTAMENTE" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Archivos generados:" -ForegroundColor White
foreach ($parte in $partesConfig) {
    Write-Host "   - $($parte.Nombre)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Como usarlos en un chat nuevo:" -ForegroundColor Cyan
Write-Host "   1. Pega siempre: Parte 1 + CONTEXTO.md" -ForegroundColor Gray
Write-Host "   2. Pega la parte del modulo que vas a tocar:" -ForegroundColor Gray
Write-Host "        - Ventas/Compras/Finanzas -> Parte 2" -ForegroundColor Gray
Write-Host "        - Logistica/RRHH/Reportes -> Parte 3" -ForegroundColor Gray
Write-Host "        - Modulo nuevo             -> Parte 4" -ForegroundColor Gray
Write-Host "   3. Si toca todo -> pega las 4" -ForegroundColor Gray
Write-Host ""