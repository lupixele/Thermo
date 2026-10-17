# Stop and remove the ThermoHardwareHelper scheduled task
$ErrorActionPreference = 'SilentlyContinue'

Stop-Process -Name "ThermoHelper" -Force
Unregister-ScheduledTask -TaskName "ThermoHardwareHelper" -Confirm:$false

Write-Host "ThermoHardwareHelper task and process removed." -ForegroundColor Green
