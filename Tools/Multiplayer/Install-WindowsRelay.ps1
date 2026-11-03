$ErrorActionPreference='Stop'
$root='C:\Services\GuerraRelay'
New-Item -ItemType Directory -Force "$root\logs","$root\tls-data" | Out-Null
& icacls.exe "$root\logs" /grant '*S-1-5-19:(OI)(CI)M' | Out-Null
& icacls.exe "$root\tls-data" /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-19:(OI)(CI)M' | Out-Null
foreach($component in @('relay','tls')){
    $name="GuerraEmpujes-$component"
    if(Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue){throw "Existing task $name"}
    $action=New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File C:\Services\GuerraRelay\Run-Server.ps1 -Component $component" -WorkingDirectory $root
    $principal=New-ScheduledTaskPrincipal -UserId 'S-1-5-19' -LogonType ServiceAccount
    $settings=New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable -MultipleInstances IgnoreNew
    Register-ScheduledTask -TaskName $name -Action $action -Trigger (New-ScheduledTaskTrigger -AtStartup) -Principal $principal -Settings $settings -Description 'Guerra de empujes multiplayer relay' | Out-Null
    Start-ScheduledTask -TaskName $name
}
if(!(Get-NetFirewallRule -Name GuerraEmpujes-TLS -ErrorAction SilentlyContinue)){
    New-NetFirewallRule -Name GuerraEmpujes-TLS -DisplayName 'Guerra de empujes TLS 9443' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 9443 -Program "$root\caddy.exe" | Out-Null
}
Get-ScheduledTask -TaskName GuerraEmpujes-* | Select TaskName,State
