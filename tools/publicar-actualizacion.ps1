<#
.SYNOPSIS
  Firma el instalador ya generado y (opcional) lo publica como Release en GitHub para que los sistemas lo detecten.

.EXAMPLE
  .\tools\publicar-actualizacion.ps1 -Version 1.0.8 -Notas "Monedas, deducciones y actualizaciones por internet" -Publicar

.NOTES
  - Requiere haber corrido antes tools\generar-clave-actualizaciones.ps1 y tener generado instalador\salida\Kirkenta-Setup-<version>.exe
  - Sin -Publicar solo crea instalador\salida\actualizacion.json (la firma) y te dice como subirlo a mano.
  - -Publicar usa GitHub CLI (gh). Instalar: https://cli.github.com  y correr una vez:  gh auth login
  - El release debe ir en el repositorio PUBLICO configurado en Helpers\ConfigActualizaciones.cs.
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notas = "",
    [switch]$Publicar
)
$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d{1,4}\.\d{1,4}\.\d{1,4}$') { throw "La version debe ser X.Y.Z (ej. 1.0.8)" }

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$salida = Join-Path $raiz "instalador\salida"
$archivo = "Kirkenta-Setup-$Version.exe"
$exe = Join-Path $salida $archivo
$priv = Join-Path $PSScriptRoot "secretos\actualizaciones-privada.xml"
$cs = Join-Path $raiz "Helpers\ConfigActualizaciones.cs"

if (-not (Test-Path $exe)) { throw "No existe $exe . Genera primero el instalador." }
if (-not (Test-Path $priv)) { throw "No existe la clave privada. Corre primero tools\generar-clave-actualizaciones.ps1" }

$codigo = [IO.File]::ReadAllText($cs)
$repo = [regex]::Match($codigo, 'Repositorio = "([^"]*)"').Groups[1].Value
$pubEnApp = [regex]::Match($codigo, 'ClavePublicaXml = "([^"]*)"').Groups[1].Value
if (-not $repo -or -not $pubEnApp) { throw "Helpers\ConfigActualizaciones.cs no tiene repositorio/clave publica. Corre tools\generar-clave-actualizaciones.ps1" }

$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.PersistKeyInCsp = $false
$rsa.FromXmlString([IO.File]::ReadAllText($priv))
if ($rsa.ToXmlString($false) -ne $pubEnApp) {
    throw "La clave privada NO corresponde a la clave publica que lleva el sistema (Helpers\ConfigActualizaciones.cs). Los clientes rechazarian esta actualizacion."
}

$sha = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$bytes = (Get-Item $exe).Length
$texto = "KIRKENTA-UPDATE|$Version|$archivo|$sha"
$firma = [Convert]::ToBase64String($rsa.SignData([Text.Encoding]::UTF8.GetBytes($texto), "SHA256"))

$manifiesto = [ordered]@{
    version = $Version
    archivo = $archivo
    sha256  = $sha
    bytes   = $bytes
    fecha   = (Get-Date -Format "yyyy-MM-dd")
    notas   = $Notas
    firma   = $firma
}
$json = Join-Path $salida "actualizacion.json"
[IO.File]::WriteAllText($json, ($manifiesto | ConvertTo-Json), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Firmado: $json" -ForegroundColor Green

if ($Publicar) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "No encuentro GitHub CLI (gh). Instalalo desde https://cli.github.com o sube los 2 archivos a mano (ver abajo)." }
    $notasRelease = if ($Notas) { $Notas } else { "Kirkenta $Version" }
    # Las notas van por archivo: si llevan comillas (") PowerShell las parte en varios argumentos y gh falla
    $archivoNotas = Join-Path $salida "notas-release.txt"
    [IO.File]::WriteAllText($archivoNotas, $notasRelease, (New-Object System.Text.UTF8Encoding($false)))
    & gh release create "v$Version" $exe $json --repo $repo --title "Kirkenta $Version" --notes-file $archivoNotas
    Remove-Item $archivoNotas -ErrorAction SilentlyContinue
    if ($LASTEXITCODE -ne 0) { throw "gh no pudo crear el release." }
    Write-Host "Publicado en https://github.com/$repo/releases/tag/v$Version . Los sistemas lo detectaran en las proximas 12 horas (o con 'Buscar actualizaciones')." -ForegroundColor Green
} else {
    Write-Host "`nPara publicar a mano: https://github.com/$repo/releases/new  -> etiqueta v$Version -> adjunta estos 2 archivos:" -ForegroundColor Yellow
    Write-Host "  $exe"
    Write-Host "  $json"
}
