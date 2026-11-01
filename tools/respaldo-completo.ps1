<#
.SYNOPSIS
  Respaldo COMPLETO de Kirkenta para recuperarse si se danya o se pierde el servidor:
  base de datos + archivos subidos (wwwroot\uploads, App_Data, storage) + codigo fuente + manifiesto.
  Todo en UN solo .zip, con verificacion, copia opcional a otro disco/nube y rotacion.

.EXAMPLE
  .\tools\respaldo-completo.ps1
  .\tools\respaldo-completo.ps1 -CopiaExterna "E:\RespaldosKirkenta"
  .\tools\respaldo-completo.ps1 -ArchivoOpciones "C:\Kirkenta-secretos\mysql-respaldo.cnf" -CopiaExterna "D:\Nube\Kirkenta"
  .\tools\respaldo-completo.ps1 -IncluirConfiguracion     # incluye appsettings*.json (trae la clave de MySQL: guarda el zip en lugar seguro)

.NOTES
  - La base de datos se respalda con tools\respaldo-bd.ps1 (mysqldump verificado).
  - Por defecto NO se incluye appsettings*.json porque contiene la clave de MySQL. Usa -IncluirConfiguracion si quieres
    poder levantar el servidor sin reescribir la cadena de conexion.
  - Para restaurar: tools\restaurar-sistema.ps1 (ver tools\LEEME-RECUPERACION.md).
#>
param(
    [string]$Destino = (Join-Path $PSScriptRoot "..\respaldos-completos"),
    [string]$CopiaExterna = "",
    [string]$ArchivoOpciones = "",
    [switch]$IncluirConfiguracion,
    [switch]$SinCodigo,
    [int]$Retener = 8
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
$Destino = (Resolve-Path $Destino).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$zipFinal = Join-Path $Destino "kirkenta-completo-$stamp.zip"
$staging = Join-Path ([IO.Path]::GetTempPath()) ("kk-completo-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null

function Copiar-Carpeta($origen, $destino, $excluirDirs = @(), $excluirArchivos = @()) {
    if (-not (Test-Path $origen)) { return 0 }
    $a = @($origen, $destino, "/E", "/R:1", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP")
    if ($excluirDirs.Count -gt 0) { $a += "/XD"; $a += $excluirDirs }
    if ($excluirArchivos.Count -gt 0) { $a += "/XF"; $a += $excluirArchivos }
    & robocopy @a | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy fallo copiando $origen (codigo $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
    return (Get-ChildItem $destino -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
}

try {
    Write-Host "1/5 Respaldando la base de datos..." -ForegroundColor Cyan
    $bdDir = Join-Path $staging "bd"
    $paramsBd = @{ Destino = $bdDir }
    if ($ArchivoOpciones) { $paramsBd["ArchivoOpciones"] = $ArchivoOpciones }
    & (Join-Path $PSScriptRoot "respaldo-bd.ps1") @paramsBd
    $zipBd = Get-ChildItem $bdDir -Filter "*.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $zipBd) { throw "No se genero el respaldo de la base de datos." }

    Write-Host "2/5 Copiando archivos subidos (documentos, imagenes, configuracion del asistente)..." -ForegroundColor Cyan
    $nUploads = Copiar-Carpeta (Join-Path $raiz "wwwroot\uploads") (Join-Path $staging "archivos\wwwroot\uploads")
    $nAppData = Copiar-Carpeta (Join-Path $raiz "App_Data") (Join-Path $staging "archivos\App_Data")
    $nStorage = Copiar-Carpeta (Join-Path $raiz "storage") (Join-Path $staging "archivos\storage")
    $nImagenes = Copiar-Carpeta (Join-Path $raiz "wwwroot\images") (Join-Path $staging "archivos\wwwroot\images")

    $incluyeConfig = $false
    if ($IncluirConfiguracion) {
        Write-Host "   Incluyendo appsettings*.json (contiene la clave de MySQL)" -ForegroundColor Yellow
        $cfg = Join-Path $staging "configuracion"
        New-Item -ItemType Directory -Path $cfg | Out-Null
        Get-ChildItem $raiz -Filter "appsettings*.json" | ForEach-Object { Copy-Item $_.FullName $cfg }
        $incluyeConfig = $true
    }

    $nCodigo = 0
    if (-not $SinCodigo) {
        Write-Host "3/5 Copiando el codigo fuente..." -ForegroundColor Cyan
        $nCodigo = Copiar-Carpeta $raiz (Join-Path $staging "codigo") `
            @("bin", "obj", ".git", ".vs", "respaldos", "respaldos-completos", "node_modules", "logs", "uploads", "App_Data", "storage") `
            @("PROYECTO.md", "PROYECTO_PARTE_*.md", "backup_*.sql", "*.zip", "appsettings*.json", "*.log")
    } else { Write-Host "3/5 Codigo omitido (-SinCodigo)" -ForegroundColor DarkGray }

    Write-Host "4/5 Armando el manifiesto y el .zip final..." -ForegroundColor Cyan
    $manifiesto = [ordered]@{
        sistema         = "Kirkenta ERP"
        fecha           = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        equipo          = $env:COMPUTERNAME
        usuario         = $env:USERNAME
        baseDatosZip    = $zipBd.Name
        baseDatosBytes  = $zipBd.Length
        baseDatosSha256 = (Get-FileHash $zipBd.FullName -Algorithm SHA256).Hash
        archivosSubidos = $nUploads
        archivosAppData = $nAppData
        archivosStorage = $nStorage
        imagenes        = $nImagenes
        archivosCodigo  = $nCodigo
        incluyeConfig   = $incluyeConfig
        nota            = "Restaurar con tools\restaurar-sistema.ps1. Requiere .NET 10 SDK y MySQL."
    }
    $manifiesto | ConvertTo-Json | Set-Content -Path (Join-Path $staging "MANIFEST.json") -Encoding UTF8

    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipFinal -CompressionLevel Optimal -Force

    # Verificacion: el zip abre, tiene entradas y trae la base de datos
    $z = [IO.Compression.ZipFile]::OpenRead($zipFinal)
    try {
        $entradas = $z.Entries.Count
        $tieneBd = ($z.Entries | Where-Object { $_.FullName -like "bd*" -and $_.FullName -like "*.zip" }).Count -gt 0
        $tieneManifiesto = ($z.Entries | Where-Object { $_.Name -eq "MANIFEST.json" }).Count -gt 0
    } finally { $z.Dispose() }
    if ($entradas -lt 2 -or -not $tieneBd -or -not $tieneManifiesto) {
        Remove-Item $zipFinal -Force
        throw "Respaldo completo invalido: el .zip no trae la base de datos o el manifiesto."
    }
    $mb = [math]::Round((Get-Item $zipFinal).Length / 1MB, 1)
    Write-Host "Respaldo completo verificado ($entradas entradas, $mb MB): $zipFinal" -ForegroundColor Green

    if ($CopiaExterna) {
        Write-Host "5/5 Copiando a $CopiaExterna ..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Force -Path $CopiaExterna | Out-Null
        $copia = Join-Path $CopiaExterna (Split-Path $zipFinal -Leaf)
        Copy-Item $zipFinal $copia -Force
        if ((Get-FileHash $copia -Algorithm SHA256).Hash -ne (Get-FileHash $zipFinal -Algorithm SHA256).Hash) {
            throw "La copia externa no coincide con el original: $copia"
        }
        Write-Host "Copia externa verificada: $copia" -ForegroundColor Green
        Get-ChildItem $CopiaExterna -Filter "kirkenta-completo-*.zip" | Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $Retener | ForEach-Object { Remove-Item $_.FullName -Force }
    } else {
        Write-Host "5/5 Sin copia externa. IMPORTANTE: copia el .zip a otro disco o a la nube; si el servidor se pierde, este respaldo se pierde con el." -ForegroundColor Yellow
    }

    Get-ChildItem $Destino -Filter "kirkenta-completo-*.zip" | Sort-Object LastWriteTime -Descending |
        Select-Object -Skip $Retener | ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Eliminado respaldo antiguo: $($_.Name)" -ForegroundColor DarkGray }
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}
