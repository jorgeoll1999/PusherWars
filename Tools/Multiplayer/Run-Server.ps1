param([ValidateSet('relay','tls')][string]$Component)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
Set-Location $root
$env:PORT='9006'
$exe=if($Component -eq 'relay'){'C:\Program Files\nodejs\node.exe'}else{Join-Path $root 'caddy.exe'}
$arguments=if($Component -eq 'relay'){@('server.mjs')}else{@('run','--config',(Join-Path $root 'Caddyfile'),'--adapter','caddyfile')}
$log=Join-Path $root "logs\$Component.log"
while($true){
    if((Test-Path $log) -and (Get-Item $log).Length -gt 5MB){
        Move-Item -LiteralPath $log -Destination "$log.previous" -Force
    }
    try{
        Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -Wait -RedirectStandardOutput "$log.stdout" -RedirectStandardError "$log.stderr"
    }catch{
        Add-Content -LiteralPath $log -Value $_.Exception.Message
    }
    Start-Sleep -Seconds 5
}
