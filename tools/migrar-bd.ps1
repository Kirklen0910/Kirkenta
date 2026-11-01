<#
.SYNOPSIS
  Migracion completa de la base de datos de Kirkenta (modulos nuevos + cualquier cambio pendiente del modelo).

.DESCRIPTION
  Pasos que hace, en orden:
    1. Respaldo de la base (tools\respaldo-bd.ps1).
    2. Revisa las 11 tablas de los modulos nuevos (Activos, Produccion, Caja chica, Toma fisica, Asientos).
       Si ya existen (las crea la app al arrancar) y estan VACIAS, las borra para que la migracion las cree.
       Si alguna tiene datos, se detiene y no toca nada.
    3. dotnet ef migrations add <Nombre>
    4. Te muestra las operaciones de la migracion generada y te pide confirmar.
    5. dotnet ef database update

  IMPORTANTE: cierra la aplicacion (dotnet run / Visual Studio) antes de correrlo.

.EXAMPLE
  .\tools\migrar-bd.ps1
  .\tools\migrar-bd.ps1 -Nombre ModulosNuevos
#>
param(
    [string]$Nombre = "ModulosNuevos",
    [string]$Appsettings = (Join-Path $PSScriptRoot "..\appsettings.json"),
    [string]$UsuarioMySql = "",
    [switch]$PedirClave
)

$ErrorActionPreference = "Stop"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $raiz

$tablasNuevas = @(
    "ActivosFijos", "DepreciacionesActivo", "MantenimientosActivo",
    "OrdenesProduccion", "DetallesOrdenProduccion", "InspeccionesCalidad",
    "GastosCajaChica", "TomasFisicas", "DetallesTomaFisica",
    "AsientosContables", "DetallesAsiento"
)

# Si mysql / mysqldump no estan en el PATH, los busca en las carpetas comunes de MySQL y los agrega solo a esta sesion
if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) {
    $candidatas = @()
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}, "C:\xampp", "C:\wamp64", "C:\laragon")) {
        if ($base -and (Test-Path $base)) {
            $candidatas += Get-ChildItem $base -Recurse -Filter mysql.exe -ErrorAction SilentlyContinue -Depth 5 |
                Select-Object -ExpandProperty DirectoryName
        }
    }
    $carpeta = $candidatas | Where-Object { Test-Path (Join-Path $_ "mysqldump.exe") } | Select-Object -First 1
    if ($carpeta) {
        $env:Path = "$carpeta;$env:Path"
        Write-Host "Usando MySQL en: $carpeta" -ForegroundColor DarkGray
    }
}

foreach ($cmd in @("dotnet", "mysql", "mysqldump")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "No se encontro '$cmd' en el PATH." }
}
& dotnet ef --version | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falta la herramienta dotnet-ef. Instalala con: dotnet tool install --global dotnet-ef" }

# ---- Conexion (lee appsettings.json, no lo modifica) ----
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

function Invoke-Sql([string]$consulta) {
    $env:MYSQL_PWD = $clave
    try {
        $salida = & mysql --host=$servidor --port=$puerto --user=$usuario --batch --skip-column-names --default-character-set=utf8mb4 $bd -e $consulta
        if ($LASTEXITCODE -ne 0) { throw "mysql fallo con codigo $LASTEXITCODE" }
        return $salida
    }
    finally { Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue }
}

# ---- 1. Respaldo ----
Write-Host "`n[1/5] Respaldo de la base de datos..." -ForegroundColor Cyan
$env:KIRKENTA_DB_USUARIO = $usuario
$env:KIRKENTA_DB_CLAVE = $clave
try { & (Join-Path $PSScriptRoot "respaldo-bd.ps1") }
finally {
    Remove-Item Env:\KIRKENTA_DB_USUARIO -ErrorAction SilentlyContinue
    Remove-Item Env:\KIRKENTA_DB_CLAVE -ErrorAction SilentlyContinue
}

# ---- 2. Tablas de modulos nuevos ----
Write-Host "`n[2/5] Revisando tablas de los modulos nuevos..." -ForegroundColor Cyan
$existentes = @(Invoke-Sql "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()")
$aBorrar = @()
foreach ($t in $tablasNuevas) {
    if ($existentes -contains $t) {
        $filas = [int](Invoke-Sql "SELECT COUNT(*) FROM ``$t``")
        if ($filas -gt 0) {
            throw "La tabla $t ya tiene $filas registro(s). No se borra nada. Pidele ayuda a Claude para migrar sin perder esos datos."
        }
        $aBorrar += $t
    }
}
foreach ($t in $aBorrar) {
    Invoke-Sql "DROP TABLE ``$t``" | Out-Null
    Write-Host "  Tabla vacia $t borrada (la recreara la migracion)." -ForegroundColor DarkGray
}

# ---- 3. Crear migracion ----
Write-Host "`n[3/5] Creando migracion '$Nombre'..." -ForegroundColor Cyan
$yaExiste = Get-ChildItem (Join-Path $raiz "Migrations") -Filter "*_$Nombre.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
if ($yaExiste) {
    Write-Host "  La migracion '$Nombre' ya existe ($($yaExiste.Name)); se reutiliza y solo falta aplicarla." -ForegroundColor Yellow
}
else {
    & dotnet ef migrations add $Nombre
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef migrations add fallo. Corrige los errores de compilacion y vuelve a correr el script." }
}

# ---- 4. Revisar ----
$archivo = Get-ChildItem (Join-Path $raiz "Migrations") -Filter "*_$Nombre.cs" |
    Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
Write-Host "`n[4/5] Operaciones de la migracion ($($archivo.Name)):" -ForegroundColor Cyan
$ops = Select-String -Path $archivo.FullName -Pattern "migrationBuilder\.(CreateTable|AddColumn|DropColumn|DropTable|AlterColumn|RenameColumn|RenameTable|DropIndex|CreateIndex)" -AllMatches
$ops | ForEach-Object { Write-Host "  " $_.Line.Trim() }
$peligro = $ops | Where-Object { $_.Line -match "DropColumn|DropTable|AlterColumn|RenameColumn|RenameTable" }
if ($peligro) {
    Write-Host "`nATENCION: la migracion borra o cambia columnas/tablas existentes (lineas de arriba)." -ForegroundColor Yellow
}
$resp = Read-Host "`nAplicar esta migracion a la base de datos? (escribe SI para continuar)"
if ($resp -ne "SI") {
    Write-Host "Cancelado. La migracion quedo creada pero NO aplicada. Para descartarla: dotnet ef migrations remove" -ForegroundColor Yellow
    exit 1
}

# ---- 5. Aplicar ----
Write-Host "`n[5/5] Aplicando migracion..." -ForegroundColor Cyan
$conexion = "Server=$servidor;Port=$puerto;Database=$bd;User=$usuario;Password=$clave;"
& dotnet ef database update --connection $conexion
if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update fallo. Tienes el respaldo del paso 1 para restaurar." }

Write-Host "`nListo. Ahora arranca la app y confirma en el log: '[Arranque] Esquema de base de datos completo.'" -ForegroundColor Green
