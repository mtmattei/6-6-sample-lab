param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [Parameter(Mandatory = $true)][bool]$Anchoring,
    [Parameter(Mandatory = $true)][string]$OutDir,
    [int]$Steps = 7,
    [int]$FramesPerStep = 3
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Cap {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    [StructLayout(LayoutKind.Sequential)] public struct R { public int L, T, Rt, B; }
}
'@

$auto = [System.Windows.Automation.AutomationElement]
$scope = [System.Windows.Automation.TreeScope]::Descendants

function Find-ById($root, $id) {
    $root.FindFirst($scope, (New-Object System.Windows.Automation.PropertyCondition($auto::AutomationIdProperty, $id)))
}
function Find-ByName($root, $name) {
    $root.FindFirst($scope, (New-Object System.Windows.Automation.PropertyCondition($auto::NameProperty, $name)))
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
Get-ChildItem $OutDir -Filter "*.png" | Remove-Item -Force -Confirm:$false

$hwnd = (Get-Process -Id $ProcessId).MainWindowHandle
$win = $auto::RootElement.FindFirst(
    [System.Windows.Automation.TreeScope]::Children,
    (New-Object System.Windows.Automation.PropertyCondition($auto::ProcessIdProperty, $ProcessId)))

# Reset to a known state, then set anchoring for this run.
(Find-ByName $win 'Reset').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 900

$sw = Find-ById $win 'AnchoringSwitch'
$toggle = $sw.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
$want = if ($Anchoring) { 'On' } else { 'Off' }
if ($toggle.Current.ToggleState -ne $want) { $toggle.Toggle(); Start-Sleep -Milliseconds 600 }

$sv = Find-ById $win 'Sv'
Start-Sleep -Milliseconds 400

# Crop to the feed card, in window-relative coordinates.
$wr = New-Object Cap+R
[void][Cap]::GetWindowRect($hwnd, [ref]$wr)
$b = $sv.Current.BoundingRectangle
$pad = 14
$crop = New-Object System.Drawing.Rectangle(
    [int]($b.X - $wr.L - $pad),
    [int]($b.Y - $wr.T - $pad),
    [int]($b.Width + $pad * 2),
    [int]($b.Height + $pad * 2))

$insert = Find-ByName $win 'Insert 5 above'
$invoke = $insert.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)

$n = 0
function Save-Frame {
    $script:n++
    $w = $wr.Rt - $wr.L; $h = $wr.B - $wr.T
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [void][Cap]::PrintWindow($hwnd, $hdc, 0x2)   # PW_RENDERFULLCONTENT
    $g.ReleaseHdc($hdc); $g.Dispose()
    $sub = $bmp.Clone($crop, 'Format32bppArgb')
    $sub.Save((Join-Path $OutDir ("f{0:D3}.png" -f $script:n)), [System.Drawing.Imaging.ImageFormat]::Png)
    $sub.Dispose(); $bmp.Dispose()
}

# Hold on the starting state so the viewer can register it before anything moves.
1..$FramesPerStep | ForEach-Object { Save-Frame; Start-Sleep -Milliseconds 120 }

for ($s = 0; $s -lt $Steps; $s++) {
    $invoke.Invoke()
    Start-Sleep -Milliseconds 260
    1..$FramesPerStep | ForEach-Object { Save-Frame; Start-Sleep -Milliseconds 110 }
}

$anchor = (Find-ById $win 'AnchorRun')
$count = (Find-ById $win 'CountRun')
"$($OutDir): $script:n frames · crop $($crop.Width)x$($crop.Height) · anchoring=$want"
