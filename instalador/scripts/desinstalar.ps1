<# Quita el arranque automatico, el servicio de MySQL y la regla de firewall. NO borra los datos del cliente. #>
param([Parameter(Mandatory = $true)][string]$Raiz)

$ErrorActionPreference = "SilentlyContinue"
$Raiz = $Raiz.TrimEnd('\')

Stop-ScheduledTask -TaskName "Kirkenta-Servicio"
Unregister-ScheduledTask -TaskName "Kirkenta-Servicio" -Confirm:$false
& taskkill.exe /F /IM Kirkenta.exe | Out-Null

Stop-Service -Name "KirkentaMySQL" -Force
Start-Sleep -Seconds 3
$mysqld = Join-Path $Raiz "mysql\bin\mysqld.exe"
if (Test-Path $mysqld) { & $mysqld --remove "KirkentaMySQL" | Out-Null }

Get-NetFirewallRule -DisplayName "Kirkenta HTTP" | Remove-NetFirewallRule
Get-NetFirewallRule -DisplayName "Kirkenta HTTPS" | Remove-NetFirewallRule

# Quita el certificado propio de Kirkenta de las entidades de confianza de esta computadora
Get-ChildItem "Cert:\LocalMachine\Root" | Where-Object { $_.Subject -eq "CN=Kirkenta ERP" } | Remove-Item -Force
exit 0
