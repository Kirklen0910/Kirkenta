<#
.SYNOPSIS
  Restaura Kirkenta desde un respaldo completo (kirkenta-completo-*.zip) en un servidor nuevo o danyado.

.EXAMPLE
  # 1) Ver que trae el respaldo (no cambia nada):
  .\restaurar-sistema.ps1 -Archivo "E:\RespaldosKirkenta\kirkenta-completo-20261025-230000.zip"

  # 2) Restaurar codigo + archivos en C:\Kirkenta y cargar la base de datos:
  .\restaurar-sistema.ps1 -Archivo "E:\...\kirkenta-completo-20261025-230000.zip" -DestinoProyecto "C:\Kirkenta" -RestaurarBd -Usuario root

.NOTES
  - La base de datos NO se sobreescribe si ya tiene tablas, salvo que uses -Reemplazar (y escribas RESTAURAR para confirmar).
  - Despues de restaurar: instalar .NET 10 SDK y MySQL, revisar la cadena de conexion en appsettings.json y correr "dotnet run".
#>
param(
    [Parameter(Mandatory = $true)][string]$Archivo,
    [string]$DestinoProyecto = "",
    [switch]$RestaurarBd,
    [string]$Servidor = "localhost",
    [int]$Puerto = 3306,
    [string]$Usuario = "root",
    [string]$BaseDatos = "",
    [string]$ArchivoOpciones = "",
    [switch]$Reemplazar
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $Archivo)) { throw "No existe el archivo: $Archivo" }

$tmp = Join-Path ([IO.Path]::GetTempPath()) ("kk-restaurar-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    Write-Host "Abriendo el respaldo..." -ForegroundColor Cyan
    Expand-Archive -Path $Archivo -DestinationPath $tmp -Force

    $man = Join-Path $tmp "MANIFEST.json"
    if (-not (Test-Path $man)) { throw "El archivo no parece un respaldo completo de Kirkenta (falta MANIFEST.json)." }
    $m = Get-Content $man -Raw | ConvertFrom-Json
    Write-Host ("Respaldo del {0} hecho en {1}" -f $m.fecha, $m.equipo)
    Write-Host ("  Base de datos: {0} ({1} KB)" -f $m.baseDatosZip, [math]::Round($m.baseDatosBytes / 1KB))
    Write-Host ("  Archivos subidos: {0} | App_Data: {1} | storage: {2} | codigo: {3} archivos | config incluida: {4}" -f $m.archivosSubidos, $m.archivosAppData, $m.archivosStorage, $m.archivosCodigo, $m.incluyeConfig)

    $zipBd = Get-ChildItem (Join-Path $tmp "bd") -Filter "*.zip" | Select-Object -First 1
    if (-not $zipBd) { throw "El respaldo no trae la base de datos." }
    if ((Get-FileHash $zipBd.FullName -Algorithm SHA256).Hash -ne $m.baseDatosSha256) {
        throw "El respaldo de la base de datos esta danyado (la suma de verificacion no coincide)."
    }
    Write-Host "Integridad de la base de datos: OK" -ForegroundColor Green

    if (-not $DestinoProyecto -and -not $RestaurarBd) {
        Write-Host "`nSolo revision. Usa -DestinoProyecto y/o -RestaurarBd para restaurar." -ForegroundColor Yellow
        return
    }

    # ---------- Archivos y codigo ----------
    if ($DestinoProyecto) {
        New-Item -ItemType Directory -Force -Path $DestinoProyecto | Out-Null
        if (Test-Path (Join-Path $tmp "codigo")) {
            Write-Host "Restaurando codigo en $DestinoProyecto ..." -ForegroundColor Cyan
            & robocopy (Join-Path $tmp "codigo") $DestinoProyecto /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "robocopy fallo restaurando el codigo." }
            $global:LASTEXITCODE = 0
        }
        if (Test-Path (Join-Path $tmp "archivos")) {
            Write-Host "Restaurando archivos subidos y datos de la aplicacion..." -ForegroundColor Cyan
            & robocopy (Join-Path $tmp "archivos") $DestinoProyecto /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "robocopy fallo restaurando los archivos." }
            $global:LASTEXITCODE = 0
        }
        if (Test-Path (Join-Path $tmp "configuracion")) {
            Write-Host "Restaurando appsettings*.json" -ForegroundColor Cyan
            Copy-Item (Join-Path $tmp "configuracion\*") $DestinoProyecto -Force
        }
    }

    # ---------- Base de datos ----------
    if ($RestaurarBd) {
        if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) {
            foreach ($c in @("C:\Program Files\MySQL", "C:\Program Files (x86)\MySQL", "C:\xampp\mysql\bin", "C:\laragon\bin\mysql")) {
                if (Test-Path $c) {
                    $exe = Get-ChildItem $c -Recurse -Filter mysql.exe -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($exe) { $env:Path = $exe.DirectoryName + ";" + $env:Path; break }
                }
            }
        }
        if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) { throw "No se encontro mysql. Instala MySQL Server/Client y vuelve a correr." }

        if (-not $BaseDatos) {
            $BaseDatos = [IO.Path]::GetFileNameWithoutExtension($zipBd.Name) -replace '-\d{8}-\d{6}$', ''
        }
        $baseArgs = @("--host=$Servidor", "--port=$Puerto", "--default-character-set=utf8mb4")
        if ($ArchivoOpciones) { $baseArgs = @("--defaults-extra-file=$ArchivoOpciones") + $baseArgs }
        else {
            $baseArgs += "--user=$Usuario"
            $seg = Read-Host "Clave de MySQL para '$Usuario'" -AsSecureString
            $env:MYSQL_PWD = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
        }

        $existentes = & mysql @baseArgs --batch --skip-column-names -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$BaseDatos'"
        if ($LASTEXITCODE -ne 0) { throw "No pude conectarme a MySQL." }
        if ([int]$existentes -gt 0 -and -not $Reemplazar) {
            throw "La base '$BaseDatos' ya tiene $existentes tablas. No la toco. Usa -Reemplazar si de verdad quieres borrarla y restaurar."
        }
        if ([int]$existentes -gt 0) {
            $r = Read-Host "Se BORRARA la base '$BaseDatos' y se reemplazara con el respaldo. Escribe RESTAURAR para continuar"
            if ($r -ne "RESTAURAR") { throw "Cancelado." }
        }

        Expand-Archive -Path $zipBd.FullName -DestinationPath (Join-Path $tmp "sql") -Force
        $sql = Get-ChildItem (Join-Path $tmp "sql") -Filter "*.sql" | Select-Object -First 1
        if (-not $sql) { throw "El zip de la base de datos no contiene un .sql" }

        Write-Host "Creando la base '$BaseDatos' y cargando los datos (puede tardar)..." -ForegroundColor Cyan
        & mysql @baseArgs -e "DROP DATABASE IF EXISTS ``$BaseDatos``; CREATE DATABASE ``$BaseDatos`` CHARACTER SET utf8mb4;"
        if ($LASTEXITCODE -ne 0) { throw "No pude crear la base de datos." }

        $lineaArgs = ($baseArgs | ForEach-Object { '"' + $_ + '"' }) -join " "
        cmd /c "mysql $lineaArgs $BaseDatos < `"$($sql.FullName)`""
        if ($LASTEXITCODE -ne 0) { throw "La carga de la base de datos fallo." }

        $tablas = & mysql @baseArgs --batch --skip-column-names -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$BaseDatos'"
        Write-Host "Base de datos '$BaseDatos' restaurada con $tablas tablas." -ForegroundColor Green
    }

    Write-Host "`nSiguientes pasos:" -ForegroundColor Cyan
    Write-Host "  1. Instalar el SDK de .NET 10 si es un equipo nuevo."
    Write-Host "  2. Revisar la cadena de conexion en appsettings.json (servidor, base de datos, usuario y clave)."
    Write-Host "  3. En la carpeta del proyecto:  dotnet run"
    Write-Host "  4. Entrar al sistema y revisar Seguridad -> Respaldo; en Configuracion -> Asistente con IA volver a escribir la clave de API si usabas una."
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
