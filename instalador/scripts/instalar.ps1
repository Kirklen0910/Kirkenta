<#
  Instalacion de Kirkenta: base de datos MySQL propia + servicio de arranque automatico.
  Lo ejecuta el instalador (Setup.exe). Es seguro volver a correrlo (actualizaciones): no toca datos existentes.
#>
param([Parameter(Mandatory = $true)][string]$Raiz)

$ErrorActionPreference = "Stop"
$Raiz = $Raiz.TrimEnd('\')
$logDir = Join-Path $Raiz "logs-instalacion"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Start-Transcript -Path (Join-Path $logDir "instalar.log") -Append | Out-Null
# Cada paso anota cuanto tiempo lleva (sirve para ver donde se tarda una actualizacion)
$reloj = [Diagnostics.Stopwatch]::StartNew()
function Paso([string]$texto) { Write-Host ("[{0:mm\:ss}] {1}" -f $reloj.Elapsed, $texto) }
Paso "Inicio de la instalacion"

function Nueva-Clave {
    $b = New-Object byte[] 30
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    return ([Convert]::ToBase64String($b) -replace '[^A-Za-z0-9]', '').Substring(0, 20)
}

function Proteger-Archivo([string]$ruta) {
    # Solo SYSTEM y Administradores pueden leerlo (por SID, para que funcione en cualquier idioma de Windows)
    & icacls.exe $ruta /inheritance:r /grant:r "*S-1-5-18:F" "*S-1-5-32-544:F" | Out-Null
}

function Proteger-Carpeta([string]$ruta) {
    # Solo SYSTEM y Administradores (con todo lo que contiene): usuarios normales de Windows no pueden leerla
    # (si todavia no existe se crea, para que nazca ya protegida)
    if (-not (Test-Path $ruta)) { New-Item -ItemType Directory -Force -Path $ruta | Out-Null }
    # OJO: sin /T. Con "/inheritance:r ... /T" icacls le quita la herencia a CADA archivo y los deja sin permisos (nadie puede abrirlos).
    # Aqui solo se cambia la carpeta; lo que contiene hereda el permiso solo.
    & icacls.exe $ruta /inheritance:r /grant:r "*S-1-5-18:(OI)(CI)F" "*S-1-5-32-544:(OI)(CI)F" /C /Q | Out-Null
}

function Endurecer-Permisos([string]$raiz, [string]$sistema, [string]$dataDir) {
    # La carpeta nace en C:\ y hereda permisos de "Usuarios autenticados: Modificar". Eso permitiria a CUALQUIER usuario
    # de la PC reemplazar Kirkenta.exe (que corre como SYSTEM) o leer la base de datos y los respaldos.
    # 0) Normaliza todo el arbol: dueno = Administradores y permisos heredados limpios (tambien repara instalaciones con
    #    archivos que hayan quedado sin permisos por una version anterior de este script)
    #    Solo se hace una vez por equipo (marca permisos-v2.txt): recorrer todo el arbol tarda minutos y en las
    #    actualizaciones los archivos nuevos ya heredan los permisos correctos de su carpeta.
    $marcaPermisos = Join-Path $raiz "datos\permisos-v2.txt"
    if (-not (Test-Path $marcaPermisos)) {
        & icacls.exe $raiz /setowner "*S-1-5-32-544" /T /C /Q | Out-Null
        & icacls.exe $raiz /reset /T /C /Q | Out-Null
        New-Item -ItemType Directory -Force -Path (Join-Path $raiz "datos") | Out-Null
        Set-Content -Path $marcaPermisos -Value (Get-Date -Format "s") -Encoding ASCII
    }
    # 1) Raiz: SYSTEM y Administradores controlan todo; los demas usuarios solo leen y ejecutan (no pueden modificar nada).
    #    Sin /T: los archivos heredan de la carpeta.
    & icacls.exe $raiz /inheritance:r /grant:r "*S-1-5-18:(OI)(CI)F" "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-32-545:(OI)(CI)RX" /C /Q | Out-Null
    # 2) Datos sensibles: ni siquiera lectura para los usuarios normales
    foreach ($c in @($dataDir, (Join-Path $raiz "datos"), (Join-Path $raiz "logs-instalacion"),
                     (Join-Path $sistema "App_Data"), (Join-Path $sistema "respaldos"), (Join-Path $sistema "logs"),
                     (Join-Path $sistema "wwwroot\uploads"))) {
        Proteger-Carpeta $c
    }
    foreach ($f in @("appsettings.json", "appsettings.Production.json")) {
        $p = Join-Path $sistema $f
        if (Test-Path $p) { Proteger-Archivo $p }
    }
}

function Crear-CertificadoHttps([string]$raiz) {
    # Certificado propio para entrar por HTTPS desde otros equipos de la red. Devuelve @{Pfx;Clave;Cer} o $null si falla.
    try {
        $carpeta = Join-Path $raiz "datos"
        New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
        $pfx = Join-Path $carpeta "kirkenta-https.pfx"
        $cer = Join-Path $carpeta "kirkenta-https.cer"
        $clave = Nueva-Clave
        $nombres = New-Object System.Collections.Generic.List[string]
        $nombres.Add("DNS=localhost"); $nombres.Add("DNS=" + $env:COMPUTERNAME); $nombres.Add("IPAddress=127.0.0.1")
        Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object { $_.IPAddress -notlike "169.254.*" -and $_.IPAddress -ne "127.0.0.1" } |
            ForEach-Object { $nombres.Add("IPAddress=" + $_.IPAddress) }
        $ext = "2.5.29.17={text}" + ($nombres -join "&")
        $cert = New-SelfSignedCertificate -Subject "CN=Kirkenta ERP" -TextExtension @($ext) -KeyAlgorithm RSA -KeyLength 3072 `
            -HashAlgorithm SHA256 -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(5) -CertStoreLocation "Cert:\LocalMachine\My"
        $sec = ConvertTo-SecureString -String $clave -AsPlainText -Force
        Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $sec | Out-Null
        Export-Certificate -Cert $cert -FilePath $cer | Out-Null
        # Esta computadora confia en su propio certificado (los demas equipos instalan kirkenta-https.cer una sola vez)
        Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
        Remove-Item ("Cert:\LocalMachine\My\" + $cert.Thumbprint) -Force -ErrorAction SilentlyContinue
        Proteger-Archivo $pfx
        return @{ Pfx = $pfx; Clave = $clave; Cer = $cer }
    } catch {
        Write-Host ("ADVERTENCIA: no se pudo crear el certificado HTTPS (se usara HTTP): " + $_.Exception.Message)
        return $null
    }
}

try {
    $mysqlDir = Join-Path $Raiz "mysql"
    $dataDir  = Join-Path $Raiz "datos-mysql"
    $sistema  = Join-Path $Raiz "sistema"
    $puerto   = 3307
    $puertoWeb = 5114
    $puertoHttps = 5115
    $httpsActivo = $false
    $servicio = "KirkentaMySQL"
    $tarea    = "Kirkenta-Servicio"
    $mysqld   = Join-Path $mysqlDir "bin\mysqld.exe"
    $mysql    = Join-Path $mysqlDir "bin\mysql.exe"
    $mysqladmin = Join-Path $mysqlDir "bin\mysqladmin.exe"
    $ini      = Join-Path $Raiz "mysql-kirkenta.ini"

    if (-not (Test-Path $mysqld)) { throw "No se encontro MySQL en $mysqlDir" }

    # ---------- 1) Configuracion de MySQL ----------
    $b = $mysqlDir -replace '\\', '/'
    $d = $dataDir -replace '\\', '/'
    $contenido = @"
[mysqld]
basedir=$b
datadir=$d
port=$puerto
bind-address=127.0.0.1
mysqlx=0
character-set-server=utf8mb4
collation-server=utf8mb4_unicode_ci
max_connections=150
local-infile=0
secure-file-priv=NULL
"@
    Set-Content -Path $ini -Value $contenido -Encoding ASCII

    # ---------- 2) Base de datos nueva o existente ----------
    # Una instalacion esta completa cuando existe la marca (o, en instalaciones anteriores, la configuracion del sistema).
    $marca = Join-Path $Raiz "datos\bd-lista.txt"
    $prod = Join-Path $sistema "appsettings.Production.json"
    $primera = (-not (Test-Path $marca)) -and (-not (Test-Path $prod))
    if ($primera) {
        # Limpia un intento anterior que quedo a medias
        $viejo = Get-Service -Name $servicio -ErrorAction SilentlyContinue
        if ($viejo) {
            Stop-Service -Name $servicio -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
            & $mysqld --remove $servicio | Out-Null
            Start-Sleep -Seconds 2
        }
        if (Test-Path $dataDir) { Remove-Item -Recurse -Force $dataDir }
        Write-Host "Inicializando la base de datos..."
        & $mysqld "--defaults-file=$ini" --initialize-insecure --console
        if ($LASTEXITCODE -ne 0) { throw "No se pudo inicializar MySQL (codigo $LASTEXITCODE)." }
    }

    if (-not (Get-Service -Name $servicio -ErrorAction SilentlyContinue)) {
        & $mysqld --install $servicio "--defaults-file=$ini"
        if ($LASTEXITCODE -ne 0) { throw "No se pudo instalar el servicio de MySQL." }
    }
    Set-Service -Name $servicio -StartupType Automatic
    if ((Get-Service -Name $servicio).Status -ne "Running") { Start-Service -Name $servicio }

    Paso "MySQL iniciado"
    # Espera a que MySQL acepte conexiones en su puerto
    $listo = $false
    for ($i = 0; $i -lt 60; $i++) {
        try {
            $cli = New-Object System.Net.Sockets.TcpClient
            $cli.Connect("127.0.0.1", $puerto)
            $cli.Close()
            $listo = $true; break
        } catch { Start-Sleep -Seconds 2 }
    }
    if (-not $listo) {
        $est = Get-Service -Name $servicio -ErrorAction SilentlyContinue
        Write-Host ("Estado del servicio " + $servicio + ": " + $est.Status)
        Get-ChildItem -Path $dataDir -Filter "*.err" -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host ("--- " + $_.FullName + " (ultimas lineas) ---")
            Get-Content $_.FullName -Tail 40 | ForEach-Object { Write-Host $_ }
        }
        throw "MySQL no respondio a tiempo."
    }


    if ($primera) {
        Write-Host "Creando la base y el usuario del sistema..."
        $claveApp  = Nueva-Clave
        $claveRoot = Nueva-Clave
        $sql = @"
CREATE DATABASE IF NOT EXISTS Kirkenta CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'kirkenta_app'@'localhost' IDENTIFIED BY '$claveApp';
CREATE USER IF NOT EXISTS 'kirkenta_app'@'127.0.0.1' IDENTIFIED BY '$claveApp';
GRANT ALL PRIVILEGES ON Kirkenta.* TO 'kirkenta_app'@'localhost';
GRANT ALL PRIVILEGES ON Kirkenta.* TO 'kirkenta_app'@'127.0.0.1';
ALTER USER 'root'@'localhost' IDENTIFIED BY '$claveRoot';
FLUSH PRIVILEGES;
"@
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $sql | & $mysql --protocol=tcp "--host=127.0.0.1" "--port=$puerto" "--user=root" --default-character-set=utf8mb4 2>&1 | ForEach-Object { Write-Host $_ }
        $codigoSql = $LASTEXITCODE
        $ErrorActionPreference = $prevEap
        if ($codigoSql -ne 0) { throw "No se pudo crear la base de datos del sistema (codigo $codigoSql)." }

        New-Item -ItemType Directory -Force -Path (Join-Path $Raiz "datos") | Out-Null
        $rootClaveArchivo = Join-Path $Raiz "datos\mysql-root.txt"
        Set-Content -Path $rootClaveArchivo -Value $claveRoot -Encoding ASCII
        Proteger-Archivo $rootClaveArchivo

        # Clave que CIFRA los respaldos (se entrega al cliente para que la guarde FUERA de esta computadora)
        $claveRespaldos = Nueva-Clave
        $cfg = @{
            ConnectionStrings = @{ DefaultConnection = "Server=127.0.0.1;Port=$puerto;Database=Kirkenta;User=kirkenta_app;Password=$claveApp;" }
            Seguridad = @{ CookieSoloHttps = $false; ClaveRespaldos = $claveRespaldos }
            Inicial = @{ AdminUsuario = "admin"; AdminClave = "" }
        }

        # HTTPS en la red local: HTTP queda solo para esta computadora y los demas equipos entran por https://IP:5115
        $https = Crear-CertificadoHttps $Raiz
        if ($https) {
            $cfg.Kestrel = @{ Endpoints = @{
                Http  = @{ Url = "http://127.0.0.1:$puertoWeb" }
                Https = @{ Url = "https://0.0.0.0:$puertoHttps"; Certificate = @{ Path = $https.Pfx; Password = $https.Clave } }
            } }
            $cfg.Seguridad.PuertoHttps = $puertoHttps
            $httpsActivo = $true
        }

        $cfg | ConvertTo-Json -Depth 8 | Set-Content -Path $prod -Encoding UTF8
        Proteger-Archivo $prod
        Set-Content -Path $marca -Value (Get-Date -Format "s") -Encoding ASCII

        Set-Content -Path (Join-Path $Raiz "datos\clave-respaldos.txt") -Value $claveRespaldos -Encoding ASCII
        Proteger-Archivo (Join-Path $Raiz "datos\clave-respaldos.txt")
        $aviso = "CLAVE DE LOS RESPALDOS: $claveRespaldos`r`nGuardala FUERA de esta computadora (papel o gestor de claves). Sin ella los respaldos NO se pueden restaurar."
        if ($https) { $aviso += "`r`n`r`nPara entrar desde otros equipos use https://IP-DE-ESTA-PC:$puertoHttps e instale una sola vez el archivo " + $https.Cer }
        Set-Content -Path (Join-Path $Raiz "datos\aviso-claves.txt") -Value $aviso -Encoding ASCII
        Proteger-Archivo (Join-Path $Raiz "datos\aviso-claves.txt")
    }
    elseif (-not (Test-Path $prod)) {
        throw "Falta $prod (configuracion de la base de datos). Contacta a soporte."
    }
    else {
        # Actualizacion de una instalacion anterior: si todavia no tiene clave de respaldos, se crea una
        try {
            $actual = Get-Content $prod -Raw | ConvertFrom-Json
            $tiene = $actual.Seguridad -and $actual.Seguridad.ClaveRespaldos
            if (-not $tiene) {
                $claveRespaldos = Nueva-Clave
                if (-not $actual.Seguridad) { $actual | Add-Member -NotePropertyName Seguridad -NotePropertyValue ([pscustomobject]@{}) -Force }
                $actual.Seguridad | Add-Member -NotePropertyName ClaveRespaldos -NotePropertyValue $claveRespaldos -Force
                $actual | ConvertTo-Json -Depth 8 | Set-Content -Path $prod -Encoding UTF8
                Proteger-Archivo $prod
                New-Item -ItemType Directory -Force -Path (Join-Path $Raiz "datos") | Out-Null
                Set-Content -Path (Join-Path $Raiz "datos\clave-respaldos.txt") -Value $claveRespaldos -Encoding ASCII
                Proteger-Archivo (Join-Path $Raiz "datos\clave-respaldos.txt")
                $aviso = "CLAVE DE LOS RESPALDOS (nueva): $claveRespaldos`r`nGuardala FUERA de esta computadora (papel o gestor de claves). Sin ella los respaldos NO se pueden restaurar."
                Set-Content -Path (Join-Path $Raiz "datos\aviso-claves.txt") -Value $aviso -Encoding ASCII
                Proteger-Archivo (Join-Path $Raiz "datos\aviso-claves.txt")
            }
            $httpsActivo = [bool]($actual.Kestrel -and $actual.Kestrel.Endpoints -and $actual.Kestrel.Endpoints.Https)
        } catch {
            Write-Host ("ADVERTENCIA: no se pudo revisar la clave de respaldos: " + $_.Exception.Message)
        }
    }

    # ---------- 2b) Permisos de carpetas (nadie mas que el administrador puede tocar el sistema ni los datos) ----------
    Paso "Permisos de carpetas..."
    Endurecer-Permisos $Raiz $sistema $dataDir
    Paso "Permisos listos"

    # ---------- 3) Arranque automatico del sistema ----------
    $accion = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c `"$Raiz\mantenimiento\Iniciar-Servicio.cmd`""
    $disparo = New-ScheduledTaskTrigger -AtStartup
    $disparo.Delay = "PT30S"
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
    $ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -StartWhenAvailable
    Register-ScheduledTask -TaskName $tarea -Action $accion -Trigger $disparo -Principal $principal -Settings $ajustes -Force | Out-Null

    # ---------- 4) Firewall (para entrar desde otros equipos de la red) ----------
    Get-NetFirewallRule -DisplayName "Kirkenta HTTP" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    Get-NetFirewallRule -DisplayName "Kirkenta HTTPS" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    if ($httpsActivo) {
        # Con HTTPS activo el puerto HTTP solo escucha en esta computadora: en la red solo se abre el HTTPS
        New-NetFirewallRule -DisplayName "Kirkenta HTTPS" -Direction Inbound -Protocol TCP -LocalPort $puertoHttps -Action Allow -Profile Private, Domain | Out-Null
    } else {
        New-NetFirewallRule -DisplayName "Kirkenta HTTP" -Direction Inbound -Protocol TCP -LocalPort $puertoWeb -Action Allow -Profile Private, Domain | Out-Null
    }

    # ---------- 5) Arrancar y esperar ----------
    Paso "Tarea de arranque y firewall listos"
    Write-Host "Iniciando Kirkenta..."
    Start-ScheduledTask -TaskName $tarea
    $arriba = $false
    for ($i = 0; $i -lt 90; $i++) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:5114/Licencia" -UseBasicParsing -TimeoutSec 5 -MaximumRedirection 0 -ErrorAction Stop
            $arriba = $true; break
        } catch {
            if ($_.Exception.Response -ne $null) { $arriba = $true; break }   # respondio (aunque sea con redireccion)
            Start-Sleep -Seconds 3
        }
    }
    if (-not $arriba) { Write-Host "ADVERTENCIA: el sistema aun no responde; puede tardar un poco mas en la primera vez." }

    Paso "Instalacion terminada (el sistema respondio)"
    Write-Host "Instalacion terminada."
    Stop-Transcript | Out-Null
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    try { Stop-Transcript | Out-Null } catch { }
    exit 1
}
