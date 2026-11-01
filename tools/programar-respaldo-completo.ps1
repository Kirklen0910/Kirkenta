<#
.SYNOPSIS
  Programa el respaldo COMPLETO una vez por semana (domingo) en el Programador de tareas de Windows.
  Usa el mismo archivo de credenciales que el respaldo diario (C:\Kirkenta-secretos\mysql-respaldo.cnf).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\tools\programar-respaldo-completo.ps1 -CopiaExterna "E:\RespaldosKirkenta"
  powershell -ExecutionPolicy Bypass -File .\tools\programar-respaldo-completo.ps1 -Hora "02:00" -Dia Saturday -CopiaExterna "D:\Nube\Kirkenta"
#>
param(
    [string]$CopiaExterna = "",
    [string]$Hora = "23:30",
    [string]$Dia = "Sunday",
    [string]$NombreTarea = "Kirkenta-Respaldo-Completo",
    [string]$Cnf = "C:\Kirkenta-secretos\mysql-respaldo.cnf"
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Cnf)) { throw "No existe $Cnf. Corre primero tools\programar-respaldo.ps1 (crea ese archivo)." }
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$script = Join-Path $PSScriptRoot "respaldo-completo.ps1"
$arg = "-NoProfile -ExecutionPolicy Bypass -File `"$script`" -ArchivoOpciones `"$Cnf`""
if ($CopiaExterna) { $arg += " -CopiaExterna `"$CopiaExterna`"" }
$accion = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arg -WorkingDirectory $raiz
$disparador = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $Dia -At $Hora
$ajustes = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 3)
Register-ScheduledTask -TaskName $NombreTarea -Action $accion -Trigger $disparador -Settings $ajustes `
    -Description "Respaldo completo semanal de Kirkenta (BD + archivos + codigo)" -Force | Out-Null
Write-Host "Tarea '$NombreTarea' programada: $Dia a las $Hora." -ForegroundColor Green
Write-Host "Probar ahora:  Start-ScheduledTask -TaskName $NombreTarea"
