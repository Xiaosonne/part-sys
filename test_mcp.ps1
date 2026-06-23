# Start Chrome DevTools MCP server in headless mode
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = 'powershell.exe'
$psi.Arguments = '-Command chrome-devtools-mcp --headless'
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.UseShellExecute = $false
$p = [System.Diagnostics.Process]::Start($psi)

Start-Sleep -Seconds 5

# Send tools/list request
$request = '{ "jsonrpc": "2.0", "id": 1, "method": "tools/list", "params": {} }' + "`n"
$p.StandardInput.WriteLine($request)
$p.StandardInput.Flush()

Start-Sleep -Seconds 3

# Read available output (non-blocking)
$buf = New-Object System.Char[] 20000
$read = $p.StandardOutput.Read($buf, 0, $buf.Length)
$response = -join $buf[0..($read-1)]
Write-Output "=== Tools List ==="
Write-Output $response

Write-Output "`n=== Server is running ==="

# Keep running so we can test more
$p.WaitForExit(5000)
