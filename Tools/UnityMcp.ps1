param([string]$Method='resources/read',[string]$Params='{}')
$h=@{Accept='application/json, text/event-stream';'Mcp-Session-Id'=(Get-Content "$env:TEMP\guerra-mcp-session.txt" -Raw).Trim()}
$b=@{jsonrpc='2.0';id=20;method=$Method;params=($Params|ConvertFrom-Json)}|ConvertTo-Json -Depth 30 -Compress
try { $r=(Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -Headers $h -ContentType 'application/json' -Body $b -ErrorAction Stop).Content }
catch {
 if($_.ErrorDetails.Message -notmatch 'Session not found'){throw}
 $init=Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -Headers @{Accept='application/json, text/event-stream'} -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Codex","version":"1.0"}}}'
 $h['Mcp-Session-Id']=($init.Headers['mcp-session-id']|Select-Object -First 1)
 Set-Content "$env:TEMP\guerra-mcp-session.txt" $h['Mcp-Session-Id']
 $null=Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -Headers $h -ContentType 'application/json' -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}'
 $r=(Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -Headers $h -ContentType 'application/json' -Body $b).Content
}
foreach($line in ($r -split "`n")){if($line.StartsWith('data:')){$o=$line.Substring(5)|ConvertFrom-Json;if($o.result){$o.result|ConvertTo-Json -Depth 40 -Compress}}}
