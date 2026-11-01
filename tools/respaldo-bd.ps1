<#
.SYNOPSIS
  Respaldo de la base de datos MySQL de Kirkenta (mysqldump + compresion + rotacion).

.EXAMPLE
  .\tools\respaldo-bd.ps1
  .\tools\respaldo-bd.ps1 -Destino "D:\Respaldos\Kirkenta" -DiasRetencion 60

.NOTES
  - Lee la cadena de conexion DefaultConnection de appsettings.json (no la modifica).
  - Requiere mysqldump en el PATH (viene con MySQL Server / MySQL Workbench).
  - Para programarlo diario: Programador de tareas de Windows -> powershell.exe -File tools\respaldo-bd.ps1
#>
param(
    [string]$Destino = (Join-Path $PSScriptRoot "..\respaldos"),
    [int]$DiasRetencion = 30,
    [string]$Appsettings = (Join-Path $PSScriptRoot "..\appsettings.json"),
    [string]$UsuarioMySql = "",
    [switch]$PedirClave,
    [string]$ArchivoOpciones = ""
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command mysqldump -ErrorAction SilentlyContinue)) {
    # Busca en las carpetas habituales de MySQL antes de rendirse
    $carpetas = @("C:\Program Files\MySQL", "C:\Program Files (x86)\MySQL", "C:\xampp\mysql\bin", "C:\laragon\bin\mysql")
    foreach ($c in $carpetas) {
        if (Test-Path $c) {
            $exe = Get-ChildItem $c -Recurse -Filter mysqldump.exe -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($exe) { $env:Path = $exe.DirectoryName + ";" + $env:Path; break }
        }
    }
}
if (-not (Get-Command mysqldump -ErrorAction SilentlyContinue)) {
    throw "No se encontro mysqldump en el PATH. Instala MySQL client tools o agrega su carpeta bin al PATH."
}

$json = Get-Content $Appsettings -Raw | ConvertFrom-Json
$cs = $json.ConnectionStrings.DefaultConnection

# Igual que la app en desarrollo: si existe appsettings.Development.json con su propia conexion, esa manda
$archivoDev = Join-Path (Split-Path -Parent $Appsettings) "appsettings.Development.json"
if (Test-Path $archivoDev) {
    $jsonDev = Get-Content $archivoDev -Raw | ConvertFrom-Json
    if ($jsonDev.ConnectionStrings -and $jsonDev.ConnectionStrings.DefaultConnection) {
        $cs = $jsonDev.ConnectionStrings.DefaultConnection
        Write-Host "Usando la conexion de appsettings.Development.json" -ForegroundColor DarkGray
    }
}
if (-not $cs) { throw "No se encontro ConnectionStrings:DefaultConnection en $Appsettings" }

$p = @{}
foreach ($par in $cs.Split(";")) {
    if ($par -match "^\s*([^=]+)=(.*)$") { $p[$matches[1].Trim().ToLower()] = $matches[2].Trim() }
}
$servidor = $p["server"]; if (-not $servidor) { $servidor = $p["host"] }
$puerto   = $p["port"];   if (-not $puerto) { $puerto = "3306" }
$bd       = $p["database"]
$usuario  = $p["user"];   if (-not $usuario) { $usuario = $p["uid"] }
$clave    = $p["password"]; if (-not $clave) { $clave = $p["pwd"] }

# Si la clave del archivo no sirve, usa -PedirClave (y -UsuarioMySql si el usuario es otro)
if ($UsuarioMySql) { $usuario = $UsuarioMySql }
if ($PedirClave) {
    $seg = Read-Host "Clave de MySQL para el usuario '$usuario'" -AsSecureString
    $clave = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
}
if ($env:KIRKENTA_DB_USUARIO) { $usuario = $env:KIRKENTA_DB_USUARIO }
if ($env:KIRKENTA_DB_CLAVE) { $clave = $env:KIRKENTA_DB_CLAVE }

if (-not $servidor -or -not $bd -or -not $usuario) { throw "Cadena de conexion incompleta (server/database/user)." }

New-Item -ItemType Directory -Force -Path $Destino | Out-Null
$Destino = (Resolve-Path $Destino).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$sql = Join-Path $Destino "$bd-$stamp.sql"
$zip = Join-Path $Destino "$bd-$stamp.zip"

# La clave va por variable de entorno para no mostrarla en la lista de procesos
$argumentos = @("--host=$servidor", "--port=$puerto")
if ($ArchivoOpciones) {
    # Tarea programada: usuario y clave viven en un archivo .cnf fuera del proyecto
    if (-not (Test-Path $ArchivoOpciones)) { throw "No existe el archivo de opciones: $ArchivoOpciones" }
    $argumentos = @("--defaults-extra-file=$ArchivoOpciones") + $argumentos
} else {
    $argumentos += "--user=$usuario"
    $env:MYSQL_PWD = $clave
}
$argumentos += @("--single-transaction", "--routines", "--triggers", "--events",
                 "--default-character-set=utf8mb4", "--result-file=$sql", $bd)
try {
    & mysqldump @argumentos
    if ($LASTEXITCODE -ne 0) { throw "mysqldump fallo con codigo $LASTEXITCODE" }
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}

# ===== VERIFICACION: un respaldo vacio o cortado es peor que no tener respaldo =====
$info = Get-Item $sql
if ($info.Length -lt 2048) {
    Remove-Item $sql -Force
    throw "Respaldo invalido: el archivo pesa solo $($info.Length) bytes."
}
$cola = (Get-Content $sql -Tail 5 -ErrorAction SilentlyContinue) -join "`n"
if ($cola -notmatch "Dump completed") {
    Remove-Item $sql -Force
    throw "Respaldo invalido: el volcado no termino correctamente (falta 'Dump completed')."
}
$tablas = (Select-String -Path $sql -Pattern "^CREATE TABLE" -SimpleMatch:$false | Measure-Object).Count
if ($tablas -lt 5) {
    Remove-Item $sql -Force
    throw "Respaldo invalido: solo contiene $tablas tablas."
}

Compress-Archive -Path $sql -DestinationPath $zip -Force
Remove-Item $sql -Force
$zipInfo = Get-Item $zip
if ($zipInfo.Length -lt 512) { throw "El .zip del respaldo quedo vacio: $zip" }
Write-Host "Respaldo verificado ($tablas tablas, $([math]::Round($zipInfo.Length/1KB)) KB): $zip" -ForegroundColor Green

# Este script NO cifra: si el sistema tiene Seguridad:ClaveRespaldos, los respaldos de la app salen cifrados
# pero este .zip queda legible. Se restringe el acceso al archivo y se avisa.
$claveApp = $json.Seguridad.ClaveRespaldos
if ($jsonDev -and $jsonDev.Seguridad -and $jsonDev.Seguridad.ClaveRespaldos) { $claveApp = $jsonDev.Seguridad.ClaveRespaldos }
if ($env:Seguridad__ClaveRespaldos) { $claveApp = $env:Seguridad__ClaveRespaldos }
if ($claveApp) {
    Write-Host "AVISO: el sistema tiene clave de respaldos, pero este script genera un .zip SIN cifrar. Guardalo solo en un lugar protegido (BitLocker/carpeta privada) o usa los respaldos de la propia app (Configuracion > Respaldos), que si salen cifrados." -ForegroundColor Yellow
}
try {
    # Solo el usuario actual, SYSTEM y Administradores pueden leer el respaldo
    & icacls $zip /inheritance:r /grant:r "$($env:USERNAME):(F)" "SYSTEM:(F)" "*S-1-5-32-544:(F)" | Out-Null
} catch { }

# Rotacion
$limite = (Get-Date).AddDays(-$DiasRetencion)
Get-ChildItem $Destino -Filter "$bd-*.zip" | Where-Object { $_.LastWriteTime -lt $limite } | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "Eliminado respaldo antiguo: $($_.Name)" -ForegroundColor DarkGray
}
