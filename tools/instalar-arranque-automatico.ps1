<#
.SYNOPSIS
  Hace que Kirkenta arranque solo cuando se prende la PC servidor, y se vuelva a levantar si se cae.
  Crea una tarea en el Programador de tareas de Windows que ejecuta tools\iniciar-servidor.cmd al iniciar sesion.

  Recomendado en la PC servidor:
   - Dejar la sesion de Windows con inicio automatico (netplwiz > desmarcar "Los usuarios deben escribir su nombre y contrasena").
   - En el BIOS, activar "Restore on AC Power Loss" para que la PC prenda sola despues de un apagon.
   - MySQL ya arranca como servicio de Windows.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\tools\instalar-arranque-automatico.ps1
  powershell -ExecutionPolicy Bypass -File .\tools\instalar-arranque-automatico.ps1 -Quitar
#>
param(
    [string]$NombreTarea = "Kirkenta-Servidor",
    [switch]$Quitar
)
$ErrorActionPreference = "Stop"

if ($Quitar) {
    Unregister-ScheduledTask -TaskName $NombreTarea -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Tarea '$NombreTarea' eliminada." -ForegroundColor Green
    return
}

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$lanzador = Join-Path $PSScriptRoot "iniciar-servidor.cmd"
if (-not (Test-Path $lanzador)) { throw "No existe $lanzador" }
if (-not (Test-Path (Join-Path $raiz "appsettings.Production.json"))) {
    Write-Warning "Todavia no hay appsettings.Production.json. Corre primero tools\https-local.ps1 (como administrador) para poder entrar desde otros equipos."
}

$usuario = "$env:USERDOMAIN\$env:USERNAME"
$accion = New-ScheduledTaskAction -Execute $lanzador -WorkingDirectory $raiz
$disparador = New-ScheduledTaskTrigger -AtLogOn -User $usuario
$ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable `
    -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId $usuario -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $NombreTarea -Action $accion -Trigger $disparador -Settings $ajustes -Principal $principal `
    -Description "Arranca Kirkenta (modo servidor) al iniciar sesion y lo reinicia si se cae" -Force | Out-Null

Write-Host "Tarea '$NombreTarea' creada: Kirkenta arrancara solo al iniciar sesion ($usuario)." -ForegroundColor Green
Write-Host "Probar ahora (sin reiniciar):  Start-ScheduledTask -TaskName $NombreTarea"
Write-Host "Ver el registro:               Get-Content logs\servidor.log -Tail 50 -Wait"
Write-Host "Quitarla:                      tools\instalar-arranque-automatico.ps1 -Quitar"
