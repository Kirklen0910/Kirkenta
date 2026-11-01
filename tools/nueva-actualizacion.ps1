# Uso:  .\tools\nueva-actualizacion.ps1 -Version 1.0.4 -Publicar
# Las notas de la version se generan SOLAS: lo que haya en tools\notas-pendientes.txt (una linea por cambio)
# + las areas del sistema que cambiaron desde la ultima version publicada. Con -Notas "..." las escribes tu.
# Compila, corre las pruebas y genera el instalador para entregar al cliente.
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$MySqlZip = "",
    [switch]$SinLicencias,
    [string]$Notas = "",
    [switch]$Publicar
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

# ---------- Notas automaticas ----------
$archivoNotas  = Join-Path $PSScriptRoot "notas-pendientes.txt"
$archivoHuella = Join-Path $PSScriptRoot "ultimo-build.json"
$vineta = [string][char]0x2022

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

$huellaNueva = Get-Huella
if ([string]::IsNullOrWhiteSpace($Notas)) {
    $lineas = @()
    if (Test-Path $archivoNotas) {
        foreach ($l in (Get-Content $archivoNotas -Encoding UTF8)) {
            $t = $l.Trim().TrimStart('-', [char]0x2022).Trim()
            if ($t) { $lineas += "$vineta $t" }
        }
    }
    if (Test-Path $archivoHuella) {
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
        # Publicado bien: esta version queda como punto de comparacion y las notas pendientes pasan al historial
        $huellaNueva | ConvertTo-Json | Set-Content -Path $archivoHuella -Encoding UTF8
        if (Test-Path $archivoNotas) {
            $historial = Join-Path $PSScriptRoot "historial-notas.txt"
            Add-Content -Path $historial -Encoding UTF8 -Value ("== $Version (" + (Get-Date -Format "yyyy-MM-dd") + ") ==")
            Add-Content -Path $historial -Encoding UTF8 -Value (Get-Content $archivoNotas -Encoding UTF8)
            Clear-Content $archivoNotas
        }
    }
    else { & "$PSScriptRoot\publicar-actualizacion.ps1" -Version $Version -Notas $Notas }
}

Write-Host "`nRecordatorios: 1) probar en PC de prueba con copia de BD de un cliente  2) pedir respaldo al cliente antes  3) entregar solo el .exe" -ForegroundColor Yellow
