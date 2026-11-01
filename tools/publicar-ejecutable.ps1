<#
.SYNOPSIS
  Genera una carpeta lista para entregar al cliente: Kirkenta.exe autocontenido (sin codigo fuente y sin instalar .NET).

.EXAMPLE
  .\tools\publicar-ejecutable.ps1
  .\tools\publicar-ejecutable.ps1 -Salida D:\Entrega\Kirkenta

.NOTES
  - Se niega a publicar si falta la clave publica de licencias (Services\Licencia\LicenciaClavePublica.cs),
    porque la entrega quedaria sin proteccion. Para pruebas internas: -SinLicencias.
  - El cliente necesita MySQL 8 (o usa la version Docker, que ya lo trae).
  - La carpeta resultante NO lleva la clave de la base de datos de desarrollo.
#>
param(
    [string]$Salida = "",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SinLicencias
)

$ErrorActionPreference = "Stop"
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not $Salida) { $Salida = Join-Path $raiz ("publicar\Kirkenta-" + $Runtime) }

$archivoClave = Join-Path $raiz "Services\Licencia\LicenciaClavePublica.cs"
$clave = Get-Content $archivoClave -Raw
if (($clave -match 'Valor = ""') -and (-not $SinLicencias)) {
    throw "Falta la clave publica de licencias. Ejecuta tools\licenciador.cmd llaves y pega el resultado en $archivoClave (o usa -SinLicencias solo para pruebas)."
}

if (Test-Path $Salida) { Remove-Item -Recurse -Force $Salida }
New-Item -ItemType Directory -Path $Salida | Out-Null

Write-Host "Compilando ($Runtime)..." -ForegroundColor Cyan
dotnet publish (Join-Path $raiz "Kirkenta.csproj") -c Release -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$Version -p:DebugType=none -p:DebugSymbols=false -o $Salida
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo." }

# Quita la clave de la base de datos de desarrollo
$cfg = Join-Path $Salida "appsettings.json"
if (Test-Path $cfg) {
    $txt = Get-Content $cfg -Raw
    $txt = $txt -replace '("DefaultConnection"\s*:\s*")[^"]*(")', '$1$2'
    Set-Content -Path $cfg -Value $txt -Encoding UTF8
}

# Archivos que no deben viajar
foreach ($basura in @("appsettings.Development.json", "appsettings.Production.json", "App_Data", "logs", "respaldos", "certs")) {
    $p = Join-Path $Salida $basura
    if (Test-Path $p) { Remove-Item -Recurse -Force $p }
}

# Configuracion de ejemplo para el cliente
$prod = @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=Kirkenta;User=kirkenta_app;Password=CAMBIAR_CLAVE;"
  },
  "Seguridad": { "CookieSoloHttps": false },
  "Inicial": { "AdminUsuario": "admin", "AdminClave": "" }
}
'@
Set-Content -Path (Join-Path $Salida "appsettings.Production.json") -Value $prod -Encoding UTF8

$inicio = @'
@echo off
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5114
set DOTNET_RUNNING_IN_CONTAINER=
echo Iniciando Kirkenta... (cierra esta ventana para detenerlo)
start "" http://localhost:5114
Kirkenta.exe
pause
'@
Set-Content -Path (Join-Path $Salida "Iniciar-Kirkenta.cmd") -Value $inicio -Encoding ASCII

$leeme = @'
KIRKENTA ERP - INSTALACION
1) Instala MySQL 8 y crea la base "Kirkenta" y el usuario "kirkenta_app" con todos los permisos sobre ella.
2) Abre appsettings.Production.json y pon la clave real del usuario en la cadena de conexion.
3) Ejecuta Iniciar-Kirkenta.cmd. La primera vez crea las tablas y un usuario administrador;
   su clave queda en App_Data\primer-acceso.txt (leela, cambiala dentro del sistema y borra el archivo).
4) Se abre el navegador en http://localhost:5114 . En la pantalla de Licencia copia el codigo de la computadora
   y enviaselo a tu proveedor para recibir tu licencia.
'@
Set-Content -Path (Join-Path $Salida "LEEME.txt") -Value $leeme -Encoding ASCII

Write-Host ""
Write-Host "Listo. Carpeta para entregar: $Salida" -ForegroundColor Green
