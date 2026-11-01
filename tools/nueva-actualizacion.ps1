# Uso:  .\tools\nueva-actualizacion.ps1 -Version 1.0.4 -Publicar
#       .\tools\nueva-actualizacion.ps1 -Version 1.0.4 -SoloNotas     (solo muestra las notas, no compila)
# Las notas de la version se generan SOLAS con los commits hechos desde la ultima version publicada
# (las lineas "- ..." del mensaje de cada commit, o su titulo) + lo que haya en tools\notas-pendientes.txt.
# Cada version solo trae lo nuevo: el commit publicado queda guardado en tools\ultimo-release.txt.
# Con -Notas "..." las escribes tu. Compila, corre las pruebas y genera el instalador para entregar al cliente.
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$MySqlZip = "",
    [switch]$SinLicencias,
    [string]$Notas = "",
    [switch]$Publicar,
    [switch]$SoloNotas
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

# ---------- Notas automaticas ----------
$archivoNotas   = Join-Path $PSScriptRoot "notas-pendientes.txt"
$archivoHuella  = Join-Path $PSScriptRoot "ultimo-build.json"
$archivoRelease = Join-Path $PSScriptRoot "ultimo-release.txt"
$vineta = [string][char]0x2022
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Get-Huella {
    $r = @{}
    foreach ($d in "Pages","Helpers","Services","Models","Data","Migrations","wwwroot","Program.cs") {
        $ruta = Join-Path $raiz $d
        if (-not (Test-Path $ruta)) { continue }
        if ((Get-Item $ruta).PSIsContainer) {
            $items = Get-ChildItem $ruta -Recurse -File -Include *.cs,*.cshtml,*.js,*.css -ErrorAction SilentlyContinue
        } else { $items = @(Get-Item $ruta) }
        foreach ($f in $items) {
            if ($f.FullName -match '\\(bin|obj|lib)\\') { continue }
            $rel = $f.FullName.Substring($raiz.Length + 1)
            $r[$rel] = (Get-FileHash $f.FullName -Algorithm SHA1).Hash
        }
    }
    return $r
}

function Get-Area([string]$rel) {
    $p = $rel -split '\\'
    switch ($p[0]) {
        "Pages"      { if ($p.Count -ge 3 -and $p[1] -ne "Shared") { return $p[1] } else { return "Pantallas comunes" } }
        "Migrations" { return "Base de datos" }
        "wwwroot"    { return "Estilos y scripts" }
        default      { return "Nucleo del sistema" }
    }
}

# Desde donde se cuentan las novedades: el commit de la ultima version publicada. Si aun no esta guardado,
# la fecha del ultimo release en GitHub (repositorio de Helpers\ConfigActualizaciones.cs).
function Get-DesdeRelease {
    if (Test-Path $archivoRelease) {
        $h = (Get-Content $archivoRelease -Raw).Trim()
        if ($h) {
            & git cat-file -e "$h^{commit}" 2>$null
            if ($LASTEXITCODE -eq 0) { return @{ Rango = "$h..HEAD" } }
        }
    }
    $cs = Join-Path $raiz "Helpers\ConfigActualizaciones.cs"
    $repo = [regex]::Match([IO.File]::ReadAllText($cs), 'Repositorio = "([^"]*)"').Groups[1].Value
    if ($repo -and (Get-Command gh -ErrorAction SilentlyContinue)) {
        $fecha = & gh release view --repo $repo --json publishedAt --jq .publishedAt 2>$null
        if ($LASTEXITCODE -eq 0 -and $fecha) { return @{ Desde = ([string]$fecha).Trim() } }
    }
    return @{ Rango = "HEAD"; Limite = 10 }
}

# Novedades de los commits: las lineas "- ..." del cuerpo de cada commit (las que siguen con sangria se unen
# a la anterior); si un commit no trae lineas asi, se usa su titulo sin el prefijo feat:/fix:.
function Get-NotasCommits {
    $desde = Get-DesdeRelease
    $gitArgs = @("-c", "i18n.logOutputEncoding=UTF-8", "log", "--no-merges", "--format=%x1e%s%x1f%b")
    if ($desde.Desde) { $gitArgs += "--since=$($desde.Desde)"; $gitArgs += "HEAD" } else { $gitArgs += $desde.Rango }
    if ($desde.Limite) { $gitArgs += "-n"; $gitArgs += "$($desde.Limite)" }
    $salida = (& git @gitArgs) -join "`n"
    $notas = New-Object System.Collections.Generic.List[string]
    foreach ($c in ($salida -split [char]0x1e)) {
        if (-not $c.Trim()) { continue }
        $partes = $c -split [char]0x1f, 2
        $titulo = $partes[0].Trim()
        $cuerpo = if ($partes.Count -gt 1) { $partes[1] } else { "" }
        $vinetas = New-Object System.Collections.Generic.List[string]
        foreach ($l in ($cuerpo -split "`n")) {
            if ($l -match '^\s*[-*]\s+(.+)$') { $vinetas.Add($Matches[1].Trim()) }
            elseif ($l -match '^\s{2,}\S' -and $vinetas.Count -gt 0) { $vinetas[$vinetas.Count - 1] += " " + $l.Trim() }
        }
        if ($vinetas.Count -eq 0 -and $titulo -notmatch '^(chore|docs|style|test)\b') {
            $t = $titulo -replace '^(feat|fix|refactor|perf)(\([^)]*\))?!?:\s*', ''
            if ($t) { $vinetas.Add($t.Substring(0, 1).ToUpper() + $t.Substring(1)) }
        }
        foreach ($v in $vinetas) { if (-not $notas.Contains($v)) { $notas.Add($v) } }
    }
    return $notas
}

$huellaNueva = Get-Huella
$sinCommit = @(& git status --porcelain 2>$null | Where-Object { $_ -match '\.(cs|cshtml|js|css)$' })
if ($sinCommit.Count -gt 0) {
    Write-Host ("Aviso: hay " + $sinCommit.Count + " archivo(s) de codigo sin commit. Entran en el instalador, pero sus cambios no salen en las notas. Haz commit antes para que aparezcan.") -ForegroundColor Yellow
}
if ([string]::IsNullOrWhiteSpace($Notas)) {
    $lineas = @()
    if (Test-Path $archivoNotas) {
        foreach ($l in (Get-Content $archivoNotas -Encoding UTF8)) {
            $t = $l.Trim().TrimStart('-', [char]0x2022).Trim()
            if ($t) { $lineas += "$vineta $t" }
        }
    }
    foreach ($n in (Get-NotasCommits)) { $lineas += "$vineta $n" }
    # Sin commits ni notas escritas: al menos las areas del sistema que cambiaron desde la ultima version
    if ($lineas.Count -eq 0 -and (Test-Path $archivoHuella)) {
        $previa = Get-Content $archivoHuella -Raw -Encoding UTF8 | ConvertFrom-Json
        $prev = @{}
        foreach ($p in $previa.PSObject.Properties) { $prev[$p.Name] = $p.Value }
        $cambiadas = @()
        foreach ($k in $huellaNueva.Keys) { if (-not $prev.ContainsKey($k) -or $prev[$k] -ne $huellaNueva[$k]) { $cambiadas += $k } }
        foreach ($k in $prev.Keys) { if (-not $huellaNueva.ContainsKey($k)) { $cambiadas += $k } }
        if ($cambiadas.Count -gt 0) {
            $areas = @($cambiadas | ForEach-Object { Get-Area $_ } | Sort-Object -Unique)
            $lineas += "$vineta Areas con cambios: " + ($areas -join ", ") + " (" + $cambiadas.Count + " archivos)"
        }
    }
    if ($lineas.Count -eq 0) { $lineas += "$vineta Mejoras y correcciones generales" }
    $Notas = $lineas -join "`n"
}
Write-Host "Notas de la version $Version :" -ForegroundColor Cyan
Write-Host $Notas
Write-Host ""
if ($SoloNotas) { return }

Write-Host "1/3 Compilando..." -ForegroundColor Cyan
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { throw "Fallo la compilacion. No se genera instalador." }

Write-Host "2/3 Pruebas..." -ForegroundColor Cyan
dotnet test tests\Kirkenta.Tests\Kirkenta.Tests.csproj
if ($LASTEXITCODE -ne 0) { throw "Fallaron las pruebas. No se genera instalador." }

Write-Host "3/3 Generando instalador $Version..." -ForegroundColor Cyan
$args2 = @{ Version = $Version }
if ($MySqlZip -ne "") { $args2.MySqlZip = $MySqlZip }
if ($SinLicencias) { $args2.SinLicencias = $true }
& "$PSScriptRoot\crear-instalador.ps1" @args2
if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) { throw "Fallo la generacion del instalador." }

$exe = Join-Path $raiz "instalador\salida\Kirkenta-Setup-$Version.exe"
if (Test-Path $exe) {
    Write-Host "`nListo: $exe" -ForegroundColor Green
    Write-Host ("SHA256: " + (Get-FileHash $exe -Algorithm SHA256).Hash)
}
# Actualizaciones por internet: firma el instalador (si ya generaste la clave) y, con -Publicar, lo sube a GitHub
if ((Test-Path $exe) -and (Test-Path (Join-Path $PSScriptRoot "secretos\actualizaciones-privada.xml"))) {
    if ($Publicar) {
        & "$PSScriptRoot\publicar-actualizacion.ps1" -Version $Version -Notas $Notas -Publicar
        # Publicado bien: esta version queda como punto de comparacion (archivos y commit) y las notas
        # pendientes pasan al historial; la proxima version solo mostrara lo nuevo desde aqui
        $huellaNueva | ConvertTo-Json | Set-Content -Path $archivoHuella -Encoding UTF8
        ([string](& git rev-parse HEAD)).Trim() | Set-Content -Path $archivoRelease -Encoding ASCII
        if (Test-Path $archivoNotas) {
            $historial = Join-Path $PSScriptRoot "historial-notas.txt"
            Add-Content -Path $historial -Encoding UTF8 -Value ("== $Version (" + (Get-Date -Format "yyyy-MM-dd") + ") ==")
            Add-Content -Path $historial -Encoding UTF8 -Value $Notas
            Clear-Content $archivoNotas
        }
    }
    else { & "$PSScriptRoot\publicar-actualizacion.ps1" -Version $Version -Notas $Notas }
}

Write-Host "`nRecordatorios: 1) probar en PC de prueba con copia de BD de un cliente  2) pedir respaldo al cliente antes  3) entregar solo el .exe" -ForegroundColor Yellow
