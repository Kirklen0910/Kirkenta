<#
.SYNOPSIS
  Crea (UNA SOLA VEZ) el par de claves con el que se firman las actualizaciones por internet.

.DESCRIPTION
  - Clave PRIVADA: tools\secretos\actualizaciones-privada.xml   (SOLO TUYA. No la subas a GitHub, no la compartas.
    Haz copia en un lugar seguro: si la pierdes no podras publicar actualizaciones y habra que reinstalar a mano.)
  - Clave PUBLICA: se escribe en Helpers\ConfigActualizaciones.cs (va dentro del sistema; solo sirve para VERIFICAR).
  - Repositorio: "usuario/repositorio" de GitHub donde publicas las versiones. Debe ser PUBLICO
    (recomendado: un repositorio aparte solo para releases, p. ej. "tuusuario/kirkenta-releases"; el codigo fuente sigue privado).

.EXAMPLE
  .\tools\generar-clave-actualizaciones.ps1 -Repositorio tuusuario/kirkenta-releases

.NOTES
  Despues de correrlo hay que generar una version nueva y entregarla a mano a los clientes una vez
  (esa version ya trae la pantalla de actualizaciones). Desde ahi las siguientes se instalan solas desde el sistema.
#>
param(
    [Parameter(Mandatory = $true)][string]$Repositorio,
    [switch]$Forzar
)
$ErrorActionPreference = "Stop"
if ($Repositorio -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*/[A-Za-z0-9._-]+$') { throw "El repositorio debe verse asi: usuario/repositorio" }

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dir = Join-Path $PSScriptRoot "secretos"
$priv = Join-Path $dir "actualizaciones-privada.xml"
$cs = Join-Path $raiz "Helpers\ConfigActualizaciones.cs"
if (-not (Test-Path $cs)) { throw "No encuentro $cs" }
if ((Test-Path $priv) -and (-not $Forzar)) { throw "Ya existe una clave privada en $priv. Si la cambias, los sistemas ya instalados no podran verificar tus actualizaciones. Usa -Forzar solo si estas seguro." }

New-Item -ItemType Directory -Force -Path $dir | Out-Null
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider(3072)
$rsa.PersistKeyInCsp = $false
[IO.File]::WriteAllText($priv, $rsa.ToXmlString($true), (New-Object System.Text.UTF8Encoding($false)))
$pub = $rsa.ToXmlString($false)
try { & icacls $priv /inheritance:r /grant:r "$($env:USERNAME):(F)" | Out-Null } catch { }

$texto = [IO.File]::ReadAllText($cs)
$texto = [regex]::Replace($texto, '(public const string Repositorio = ")[^"]*(";)', { param($m) $m.Groups[1].Value + $Repositorio + $m.Groups[2].Value })
$texto = [regex]::Replace($texto, '(public const string ClavePublicaXml = ")[^"]*(";)', { param($m) $m.Groups[1].Value + $pub + $m.Groups[2].Value })
[IO.File]::WriteAllText($cs, $texto, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Listo." -ForegroundColor Green
Write-Host " - Clave privada : $priv   (GUARDALA en un lugar seguro y NUNCA la subas a GitHub)" -ForegroundColor Yellow
Write-Host " - Clave publica : escrita en Helpers\ConfigActualizaciones.cs"
Write-Host " - Repositorio   : $Repositorio"
Write-Host "Ahora genera una version nueva (.\tools\nueva-actualizacion.ps1 -Version X.Y.Z) y entregala a mano una vez a cada cliente."
