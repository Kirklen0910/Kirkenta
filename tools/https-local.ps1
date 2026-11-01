<#
.SYNOPSIS
  Prepara HTTPS para usar Kirkenta en la red del negocio (otras PCs, tablets y celulares por WiFi).
  Sin HTTPS el inicio de sesion no funciona desde otros equipos (las cookies seguras lo exigen)
  y el POS sin internet tampoco se activa.

  Hace lo siguiente:
   1. Crea un certificado propio para este servidor (valido para su nombre y sus IP).
   2. Lo deja confiable en este servidor y lo exporta (certs\kirkenta-local.cer) para instalarlo en los demas equipos.
   3. Escribe appsettings.Production.json con el puerto HTTPS.
   4. Abre el puerto en el Firewall de Windows (redes privadas y de dominio).

.EXAMPLE
  # PowerShell como ADMINISTRADOR, en la carpeta del proyecto:
  powershell -ExecutionPolicy Bypass -File .\tools\https-local.ps1
  powershell -ExecutionPolicy Bypass -File .\tools\https-local.ps1 -NombresExtra "192.168.1.10","kirkenta.local" -PuertoHttps 5115
#>
param(
    [int]$PuertoHttps = 5115,
    [int]$PuertoHttp = 5114,
    [string[]]$NombresExtra = @(),
    [int]$Anios = 5,
    [string]$Clave = ""
)
$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $esAdmin) { throw "Abre PowerShell como ADMINISTRADOR (clic derecho > Ejecutar como administrador) y vuelve a correr este script." }

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$carpetaCert = Join-Path $raiz "certs"
New-Item -ItemType Directory -Force -Path $carpetaCert | Out-Null

# --- Nombres y direcciones por las que se va a entrar al sistema ---
$ipsLocales = @(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } | Select-Object -ExpandProperty IPAddress)
$extraDns = @($NombresExtra | Where-Object { $_ -notmatch '^\d+\.\d+\.\d+\.\d+$' })
$extraIps = @($NombresExtra | Where-Object { $_ -match '^\d+\.\d+\.\d+\.\d+$' })
$dns = @("localhost", $env:COMPUTERNAME) + $extraDns | Select-Object -Unique
$ips = @("127.0.0.1") + $ipsLocales + $extraIps | Select-Object -Unique
$partes = @()
foreach ($d in $dns) { $partes += "DNS=$d" }
foreach ($i in $ips) { $partes += "IPAddress=$i" }
$san = "2.5.29.17={text}" + ($partes -join "&")

# --- Certificado ---
if ([string]::IsNullOrWhiteSpace($Clave)) {
    $Clave = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | ForEach-Object { [char]$_ })
}
$cert = New-SelfSignedCertificate -Subject "CN=Kirkenta Local" -FriendlyName "Kirkenta (servidor local)" `
    -TextExtension @($san) -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears($Anios) -CertStoreLocation "Cert:\LocalMachine\My"

$pfx = Join-Path $carpetaCert "kirkenta-local.pfx"
$cer = Join-Path $carpetaCert "kirkenta-local.cer"
$segura = ConvertTo-SecureString -String $Clave -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $segura | Out-Null
Export-Certificate -Cert $cert -FilePath $cer | Out-Null
Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
Remove-Item -Path ("Cert:\LocalMachine\My\" + $cert.Thumbprint) -ErrorAction SilentlyContinue

# --- appsettings.Production.json (se conserva lo que ya tenga y solo se reemplaza lo de Kestrel) ---
$ruta = Join-Path $raiz "appsettings.Production.json"
if (Test-Path $ruta) { $cfg = Get-Content $ruta -Raw | ConvertFrom-Json } else { $cfg = [pscustomobject]@{} }
$kestrel = [pscustomobject]@{
    Endpoints = [pscustomobject]@{
        Http  = [pscustomobject]@{ Url = "http://localhost:$PuertoHttp" }
        Https = [pscustomobject]@{
            Url = "https://0.0.0.0:$PuertoHttps"
            Certificate = [pscustomobject]@{ Path = $pfx; Password = $Clave }
        }
    }
}
if ($cfg.PSObject.Properties.Name -contains "Kestrel") { $cfg.Kestrel = $kestrel } else { $cfg | Add-Member -NotePropertyName Kestrel -NotePropertyValue $kestrel }
if ($cfg.PSObject.Properties.Name -contains "https_port") { $cfg.https_port = $PuertoHttps } else { $cfg | Add-Member -NotePropertyName https_port -NotePropertyValue $PuertoHttps }
$cfg | ConvertTo-Json -Depth 10 | Set-Content -Path $ruta -Encoding UTF8

# --- Que la clave del certificado y el certificado no se suban a git ---
$gitignore = Join-Path $raiz ".gitignore"
if (Test-Path $gitignore) {
    $actual = Get-Content $gitignore -Raw
    foreach ($linea in @("certs/", "appsettings.Production.json")) {
        if ($actual -notmatch [regex]::Escape($linea)) { Add-Content -Path $gitignore -Value $linea }
    }
}

# --- Firewall ---
Remove-NetFirewallRule -DisplayName "Kirkenta HTTPS" -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName "Kirkenta HTTPS" -Direction Inbound -Protocol TCP -LocalPort $PuertoHttps -Action Allow -Profile Private, Domain | Out-Null

Write-Host ""
Write-Host "Listo. HTTPS preparado." -ForegroundColor Green
Write-Host ""
Write-Host "Direcciones para entrar al sistema:" -ForegroundColor Cyan
foreach ($i in ($ips | Where-Object { $_ -ne "127.0.0.1" })) { Write-Host "   https://${i}:$PuertoHttps" }
Write-Host "   https://$($env:COMPUTERNAME):$PuertoHttps"
Write-Host ""
Write-Host "Para que otros equipos no muestren advertencia de seguridad, instala este certificado en cada uno:" -ForegroundColor Cyan
Write-Host "   $cer"
Write-Host "   (Windows: doble clic > Instalar certificado > Equipo local > 'Entidades de certificacion raiz de confianza')"
Write-Host "   (Android: Ajustes > Seguridad > Instalar desde almacenamiento > Certificado de CA)"
Write-Host "   (iPhone: abrir el archivo > Instalar perfil > Ajustes > General > Informacion > Config. de certificados > activar)"
Write-Host ""
Write-Host "Siguiente paso: arrancar el sistema en modo servidor:  tools\iniciar-servidor.cmd" -ForegroundColor Yellow
Write-Host "Para que arranque solo al prender la PC:  tools\instalar-arranque-automatico.ps1"
