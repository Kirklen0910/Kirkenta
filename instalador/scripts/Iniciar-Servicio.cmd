@echo off
rem Arranca Kirkenta y lo vuelve a levantar solo si se cae. Lo ejecuta la tarea programada "Kirkenta-Servicio".
cd /d "%~dp0..\sistema"
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5114
:inicio
Kirkenta.exe >nul 2>>"..\logs-instalacion\errores.log"
ping -n 11 127.0.0.1 >nul
goto inicio
