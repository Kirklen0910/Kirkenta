@echo off
rem Arranca Kirkenta en modo servidor (usa appsettings.Production.json: HTTPS para la red del negocio).
cd /d "%~dp0.."
set ASPNETCORE_ENVIRONMENT=Production
if not exist logs mkdir logs
echo [%date% %time%] Iniciando Kirkenta... >> logs\servidor.log
dotnet run --no-launch-profile -c Release >> logs\servidor.log 2>&1
