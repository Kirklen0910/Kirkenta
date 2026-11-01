<#
.SYNOPSIS
  Prueba que el ultimo respaldo SI se puede restaurar (un respaldo que nunca se probo no es un respaldo).

.DESCRIPTION
  1. Toma el .zip mas reciente de la carpeta respaldos.
  2. Lo descomprime en una carpeta temporal.
  3. Lo carga en una base de datos TEMPORAL (kirkenta_prueba_restore) - nunca toca la base real.
  4. Compara cantidad de tablas y filas de tablas clave contra la base real.
  5. Borra la base temporal y la carpeta temporal.

.EXAMPLE
  .\tools\probar-restauracion.ps1
  .\tools\probar-restauracion.ps1 -ArchivoOpciones "C:\Kirkenta-secretos\mysql-respaldo.cnf"
#>
param(
    [string]$Carpeta = (Join-Path $PSScriptRoot "..\respaldos"),
    [string]$Appsettings = (Join-Path $PSScriptRoot "..\appsettings.json"),
    [string]$ArchivoOpciones = "",
    [string]$BdPrueba = "kirkenta_prueba_restore"
)

$ErrorActionPreference = "Stop"

function Buscar-Exe($nombre) {
    if (Get-Command $nombre -ErrorAction SilentlyContinue) { return }
    foreach ($c in @("C:\Program Files\MySQL", "C:\Program Files (x86)\MySQL", "C:\xampp\mysql\bin", "C:\laragon\bin\mysql")) {
        if (Test-Path $c) {
            $exe = Get-ChildItem $c -Recurse -Filter "$nombre.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($exe) { $env:Path = $exe.DirectoryName + ";" + $env:Path; return }
        }
    }
    throw "No se encontro $nombre en el PATH."
}
Buscar-Exe "mysql"

$zip = Get-ChildItem $Carpeta -Filter "*.zip" -ErrorAction Stop | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $zip) { throw "No hay respaldos .zip en $Carpeta" }
Write-Host "Probando: $($zip.Name) ($([math]::Round($zip.Length/1KB)) KB, $($zip.LastWriteTime))" -ForegroundColor Cyan

# --- conexion (igual que respaldo-bd.ps1) ---
$json = Get-Content $Appsettings -Raw | ConvertFrom-Json
$cs = $json.ConnectionStrings.DefaultConnection
$dev = Join-Path (Split-Path -Parent $Appsettings) "appsettings.Development.json"
if (Test-Path $dev) {
    $jd = Get-Content $dev -Raw | ConvertFrom-Json
    if ($jd.ConnectionStrings -and $jd.ConnectionStrings.DefaultConnection) { $cs = $jd.ConnectionStrings.DefaultConnection }
}
$p = @{}
foreach ($par in $cs.Split(";")) { if ($par -match "^\s*([^=]+)=(.*)$") { $p[$matches[1].Trim().ToLower()] = $matches[2].Trim() } }
$servidor = $p["server"]; if (-not $servidor) { $servidor = $p["host"] }
$puerto = $p["port"]; if (-not $puerto) { $puerto = "3306" }
$bdReal = $p["database"]
$usuario = $p["user"]; if (-not $usuario) { $usuario = $p["uid"] }
$clave = $p["password"]; if (-not $clave) { $clave = $p["pwd"] }
if ($env:KIRKENTA_DB_USUARIO) { $usuario = $env:KIRKENTA_DB_USUARIO }
if ($env:KIRKENTA_DB_CLAVE) { $clave = $env:KIRKENTA_DB_CLAVE }

$base = @("--host=$servidor", "--port=$puerto", "--default-character-set=utf8mb4")
if ($ArchivoOpciones) { $base = @("--defaults-extra-file=$ArchivoOpciones") + $base }
else { $base += "--user=$usuario"; $env:MYSQL_PWD = $clave }

function Sql($bd, $consulta) {
    $a = $base + @("--batch", "--skip-column-names")
    if ($bd) { $a += $bd }
    $a += @("-e", $consulta)
    $r = & mysql @a
    if ($LASTEXITCODE -ne 0) { throw "mysql fallo: $consulta" }
    return $r
}

$tmp = Join-Path ([IO.Path]::GetTempPath()) ("kirkenta-restore-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    Expand-Archive -Path $zip.FullName -DestinationPath $tmp -Force
    $sql = Get-ChildItem $tmp -Filter "*.sql" | Select-Object -First 1
    if (-not $sql) { throw "El .zip no contiene un archivo .sql" }

    Sql $null "DROP DATABASE IF EXISTS ``$BdPrueba``; CREATE DATABASE ``$BdPrueba`` CHARACTER SET utf8mb4;" | Out-Null
    Write-Host "Restaurando en la base temporal '$BdPrueba'..." -ForegroundColor Cyan

    $args2 = $base + @($BdPrueba)
    Get-Content $sql.FullName -Raw -Encoding UTF8 | & mysql @args2
    if ($LASTEXITCODE -ne 0) { throw "La restauracion fallo (mysql devolvio $LASTEXITCODE)." }

    $tablasPrueba = [int](Sql $null "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$BdPrueba'")
    $tablasReal = [int](Sql $null "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$bdReal'")
    Write-Host "Tablas: respaldo=$tablasPrueba  base real=$tablasReal"

    $tablasClave = @("Usuarios", "Clientes", "Productos", "Ventas", "Facturas", "Pagos", "MovimientosFinancieros", "Empleados")
    $filasMal = 0
    foreach ($t in $tablasClave) {
        $existe = [int](Sql $null "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$BdPrueba' AND table_name='$t'")
        if ($existe -eq 0) { Write-Host ("  {0,-24} (no existe en el respaldo)" -f $t) -ForegroundColor DarkGray; continue }
        $a = [int](Sql $BdPrueba "SELECT COUNT(*) FROM ``$t``")
        $b = [int](Sql $bdReal "SELECT COUNT(*) FROM ``$t``")
        $color = if ($a -le $b) { "Green" } else { "Yellow" }
        Write-Host ("  {0,-24} respaldo={1,8}  real={2,8}" -f $t, $a, $b) -ForegroundColor $color
        if ($a -eq 0 -and $b -gt 0) { $filasMal++ }
    }

    if ($tablasPrueba -lt 5) { throw "PRUEBA FALLIDA: el respaldo restaurado tiene solo $tablasPrueba tablas." }
    if ($filasMal -gt 0) { throw "PRUEBA FALLIDA: hay tablas con datos en la base real que quedaron vacias en el respaldo." }
    Write-Host "PRUEBA OK: el respaldo se puede restaurar y trae datos." -ForegroundColor Green
}
finally {
    try { Sql $null "DROP DATABASE IF EXISTS ``$BdPrueba``;" | Out-Null } catch { }
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}
