# Register ThermoHelper as an elevated background scheduled task
# Runs automatically at logon with highest privileges so CPU MSR temperatures
# can be sampled with zero UAC prompts.

$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Elevating to Administrator to register scheduled task..." -ForegroundColor Yellow
    Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs -Wait
    exit
}

$helperExe = "P:\Magnanimity\Projects\Thermo\Thermo\bin\Debug\net10.0-windows10.0.26100.0\win-x64\ThermoHelper.exe"

if (-not (Test-Path $helperExe)) {
    Write-Error "ThermoHelper.exe not found at $helperExe. Build the solution first."
    exit 1
}

Write-Host "Registering ThermoHardwareHelper scheduled task..." -ForegroundColor Cyan

$action = New-ScheduledTaskAction -Execute $helperExe
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero)

Register-ScheduledTask -TaskName "ThermoHardwareHelper" -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName "ThermoHardwareHelper" -ErrorAction SilentlyContinue

Write-Host "ThermoHardwareHelper successfully registered and started!" -ForegroundColor Green
Write-Host "CPU Package and Core Average temperatures will now be sampled continuously." -ForegroundColor Gray
