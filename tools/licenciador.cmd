@echo off
rem Herramienta del EMISOR de licencias. Guarda la llave privada y las licencias en %USERPROFILE%\KirkentaLicencias
rem (fuera del proyecto, para que nunca se entreguen por error). Haz copia de seguridad de esa carpeta.
setlocal
set "CARPETA=%USERPROFILE%\KirkentaLicencias"
if not exist "%CARPETA%" mkdir "%CARPETA%"
set "KIRKENTA_LLAVE=%CARPETA%\llave-privada.pem"
set "PROYECTO=%~dp0Licenciador"
pushd "%CARPETA%"
dotnet run --project "%PROYECTO%" -c Release -- %*
popd
endlocal
