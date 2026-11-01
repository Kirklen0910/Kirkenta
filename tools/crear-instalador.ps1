<#
.SYNOPSIS
  Crea Kirkenta-Setup-<version>.exe : instalador profesional (sistema + MySQL + arranque automatico) para entregar al cliente.

.DESCRIPTION
  Requisitos en TU computadora (una sola vez):
    1. Inno Setup 6 (gratis): https://jrsoftware.org/isdl.php
    2. MySQL Community Server 8.0 en version ZIP ("Windows (x86, 64-bit), ZIP Archive", NO el MSI):
       https://dev.mysql.com/downloads/mysql/   -> pasa la ruta del .zip con -MySqlZip (solo la primera vez)
    3. Opcional pero recomendado: vc_redist.x64.exe (Microsoft Visual C++ Redistributable) copiado en instalador\recursos\
       (algunas PCs sin actualizar lo necesitan para MySQL): https://aka.ms/vs/17/release/vc_redist.x64.exe

.EXAMPLE
  .\tools\crear-instalador.ps1 -MySqlZip C:\Descargas\mysql-8.0.40-winx64.zip -Version 1.0.0
  .\tools\crear-instalador.ps1 -Version 1.0.1          (las veces siguientes ya no hace falta el zip)
#>
param(
    [string]$MySqlZip = "",
    [string]$Version = "1.0.0",
    [switch]$SinLicencias,
    [switch]$PermitirOtraVersion
)

$ErrorActionPreference = "Stop"
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$inst = Join-Path $raiz "instalador"
$recursos = Join-Path $inst "recursos"
$mysqlDestino = Join-Path $recursos "mysql"

# 1) MySQL portable listo para empaquetar
if (-not (Test-Path (Join-Path $mysqlDestino "bin\mysqld.exe"))) {
    if (-not $MySqlZip -or -not (Test-Path $MySqlZip)) {
        throw "Falta MySQL. Descarga el ZIP de MySQL Community Server 8.0 (Windows 64-bit, ZIP Archive) y vuelve a correr con -MySqlZip <ruta del zip>."
    }
    Write-Host "Preparando MySQL..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $recursos | Out-Null
    $tmp = Join-Path $env:TEMP ("kk-mysql-" + [guid]::NewGuid().ToString("N"))
    Expand-Archive -Path $MySqlZip -DestinationPath $tmp -Force
    $carpeta = Get-ChildItem $tmp -Directory | Select-Object -First 1
    if (-not $carpeta -or -not (Test-Path (Join-Path $carpeta.FullName "bin\mysqld.exe"))) { throw "El ZIP no parece ser MySQL Community Server (no encuentro bin\mysqld.exe)." }
    if (Test-Path $mysqlDestino) { Remove-Item -Recurse -Force $mysqlDestino }
    Move-Item $carpeta.FullName $mysqlDestino
    Remove-Item -Recurse -Force $tmp
    foreach ($sobra in @("docs", "include", "mysql-test")) {
        $p = Join-Path $mysqlDestino $sobra
        if (Test-Path $p) { Remove-Item -Recurse -Force $p }
    }
}

# Verifica la version de MySQL que se va a empaquetar (el sistema esta probado con 8.4 LTS)
$ver = ((& (Join-Path $mysqlDestino "bin\mysqld.exe") --version) -join " ")
Write-Host ("MySQL a empaquetar: " + $ver) -ForegroundColor Cyan
if (($ver -notmatch '8\.4\.') -and (-not $PermitirOtraVersion)) {
    throw "El MySQL preparado NO es la version 8.4 ($ver). Borra la carpeta $mysqlDestino y vuelve a correr con -MySqlZip <zip de MySQL 8.4>."
}

# 2) Compila el sistema (verifica la clave de licencias y deja la carpeta limpia)
Write-Host "Compilando el sistema..." -ForegroundColor Cyan
if ($SinLicencias) { & (Join-Path $PSScriptRoot "publicar-ejecutable.ps1") -SinLicencias -Version $Version } else { & (Join-Path $PSScriptRoot "publicar-ejecutable.ps1") -Version $Version }
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) { throw "No se pudo compilar el sistema." }

# 3) Inno Setup
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "No encuentro Inno Setup 6. Instalalo desde https://jrsoftware.org/isdl.php y vuelve a correr." }

Write-Host "Creando el instalador..." -ForegroundColor Cyan
Push-Location $inst
try { & $iscc "/DVersion=$Version" "kirkenta.iss" } finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw "Inno Setup fallo." }

Write-Host ""
Write-Host "Listo: $inst\salida\Kirkenta-Setup-$Version.exe" -ForegroundColor Green
Write-Host "Este es el UNICO archivo que le llevas al cliente."
