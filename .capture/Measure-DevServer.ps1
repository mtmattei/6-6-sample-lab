param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$WorkingDirectory = "C:\Users\Platform006\A11yCapture",
    [int]$TimeoutSeconds = 120,
    [int]$InitDelaySeconds = 0
)

# Measures wall time from process start to the MCP tools/list response, which is
# what a client actually waits on before it can call a tool.
#
# 6.5 boots a DevServer on a port and then starts a separate MCP stdio proxy.
# An initialize sent at t=0 is consumed by the first transport and never
# answered, so 6.5 needs -InitDelaySeconds to hold the request until the proxy
# is listening. 6.6 serves MCP directly and needs no delay. Report the delay
# alongside the number: it is part of the measurement, not a workaround.

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
foreach ($a in @("dnx", "-y", "uno.devserver@$Version", "--mcp-app")) { [void]$psi.ArgumentList.Add($a) }
$psi.WorkingDirectory = $WorkingDirectory
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$proc = [System.Diagnostics.Process]::Start($psi)

if ($InitDelaySeconds -gt 0) { Start-Sleep -Seconds $InitDelaySeconds }

$init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"bench","version":"1.0"}}}'
$inited = '{"jsonrpc":"2.0","method":"notifications/initialized"}'
$list = '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

$proc.StandardInput.WriteLine($init)
$proc.StandardInput.Flush()

$initMs = $null
$listMs = $null
$toolCount = $null
$sentList = $false

while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    if ($proc.HasExited -and $proc.StandardOutput.EndOfStream) { break }
    $line = $proc.StandardOutput.ReadLine()
    if ($null -eq $line) { break }
    if ($line -notmatch '^\s*\{') { continue }

    try { $msg = $line | ConvertFrom-Json } catch { continue }

    if ($msg.id -eq 1 -and $null -eq $initMs) {
        $initMs = [int]$sw.Elapsed.TotalMilliseconds
        $proc.StandardInput.WriteLine($inited); $proc.StandardInput.Flush()
        $proc.StandardInput.WriteLine($list);   $proc.StandardInput.Flush()
        $sentList = $true
        continue
    }

    if ($sentList -and $msg.id -eq 2) {
        $listMs = [int]$sw.Elapsed.TotalMilliseconds
        $toolCount = @($msg.result.tools).Count
        break
    }
}

$sw.Stop()
try { if (-not $proc.HasExited) { $proc.Kill($true) } } catch { }

[pscustomobject]@{
    Version      = $Version
    InitDelaySec = $InitDelaySeconds
    InitializeMs = $initMs
    ToolsListMs  = $listMs
    Tools        = $toolCount
    TimedOut     = ($null -eq $listMs)
}
