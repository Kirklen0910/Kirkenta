<#
.SYNOPSIS
  Programa el respaldo automatico DIARIO de la base de datos (Programador de tareas de Windows).

.EXAMPLE
  # Una sola vez, en PowerShell como administrador:
  powershell -ExecutionPolicy Bypass -File .\tools\programar-respaldo.ps1
  powershell -ExecutionPolicy Bypass -File .\tools\programar-respaldo.ps1 -Hora "23:30" -Destino "D:\Respaldos\Kirkenta"

.NOTES
  - Crea (si no existe) el archivo C:\Kirkenta-secretos\mysql-respaldo.cnf con el usuario y la clave de MySQL
    que escribas aqui. Ese archivo queda FUERA del proyecto y solo lo puede leer tu usuario de Windows.
  - La tarea corre respaldo-bd.ps1, que verifica el volcado (tamano, tablas, 'Dump completed')
    y falla con error visible si el respaldo salio vacio o cortado.
#>
param(
    [string]$Hora = "23:00",
    [string]$Destino = "",
    [string]$NombreTarea = "Kirkenta-Respaldo-Diario",
    [string]$CarpetaSecretos = "C:\Kirkenta-secretos"
)

$ErrorActionPreference = "Stop"
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$script = Join-Path $PSScriptRoot "respaldo-bd.ps1"
if (-not $Destino) { $Destino = Join-Path $raiz "respaldos" }

New-Item -ItemType Directory -Force -Path $CarpetaSecretos | Out-Null
$cnf = Join-Path $CarpetaSecretos "mysql-respaldo.cnf"

if (-not (Test-Path $cnf)) {
    $u = Read-Host "Usuario de MySQL para los respaldos (ej. root)"
    $seg = Read-Host "Clave de MySQL para '$u'" -AsSecureString
    $c = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
    $contenido = "[client]`r`nuser=$u`r`npassword=`"$c`"`r`n"
    [IO.File]::WriteAllText($cnf, $contenido, (New-Object Text.UTF8Encoding($false)))
    # Solo tu usuario puede leerlo
    icacls $cnf /inheritance:r /grant:r "$($env:USERNAME):(R,W)" | Out-Null
    Write-Host "Archivo de credenciales creado: $cnf" -ForegroundColor Green
}

$accion = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$script`" -ArchivoOpciones `"$cnf`" -Destino `"$Destino`"" `
    -WorkingDirectory $raiz
$disparador = New-ScheduledTaskTrigger -Daily -At $Hora
$ajustes = New-ScheduledTaskSettingsSet -StartWhenAvailable -RunOnlyIfNetworkAvailable:$false -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Register-ScheduledTask -TaskName $NombreTarea -Action $accion -Trigger $disparador -Settings $ajustes `
    -Description "Respaldo diario verificado de la base de datos Kirkenta" -Force | Out-Null

Write-Host "Tarea '$NombreTarea' programada todos los dias a las $Hora." -ForegroundColor Green
Write-Host "Destino de los respaldos: $Destino"
Write-Host "Prueba ahora mismo con:  Start-ScheduledTask -TaskName $NombreTarea"
Write-Host "Ver resultado:           Get-ScheduledTaskInfo -TaskName $NombreTarea"
