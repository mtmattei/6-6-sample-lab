param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [int]$MaxDepth = 40
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$auto = [System.Windows.Automation.AutomationElement]
$root = $auto::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($auto::ProcessIdProperty, $ProcessId)
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if ($null -eq $window) { throw "No top-level UIA window found for process $ProcessId" }

$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
$lines = New-Object System.Collections.Generic.List[string]
$script:count = 0

function Write-Node($element, $depth) {
    if ($depth -gt $MaxDepth) { return }
    $c = $element.Current
    $indent = ' ' * ($depth * 2)
    $name = $c.Name
    if ([string]::IsNullOrWhiteSpace($name)) { $name = '' } else { $name = " `"$name`"" }
    $id = if ([string]::IsNullOrWhiteSpace($c.AutomationId)) { '' } else { "  [id=$($c.AutomationId)]" }
    $off = if ($c.IsOffscreen) { '  (offscreen)' } else { '' }

    $facts = New-Object System.Collections.Generic.List[string]
    foreach ($p in $element.GetSupportedPatterns()) {
        $pn = $p.ProgrammaticName -replace '^(\w+)PatternIdentifiers\.Pattern$', '$1'
        switch ($pn) {
            'Toggle' { $facts.Add("Toggle=$($element.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState)") }
            'RangeValue' {
                $rv = $element.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern).Current
                $facts.Add("RangeValue=$($rv.Value) [$($rv.Minimum)..$($rv.Maximum)]")
            }
            'Value' { $facts.Add("Value='$($element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value)'") }
            'SelectionItem' { $facts.Add("Selected=$($element.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected)") }
            default { $facts.Add($pn) }
        }
    }
    $pat = if ($facts.Count) { '  {' + ($facts -join ', ') + '}' } else { '' }

    $lines.Add("$indent$($c.ControlType.ProgrammaticName -replace '^ControlType\.', '')$name$id$off$pat")
    $script:count++

    $child = $walker.GetFirstChild($element)
    while ($null -ne $child) {
        Write-Node $child ($depth + 1)
        $child = $walker.GetNextSibling($child)
    }
}

Write-Node $window 0
$lines.Add('')
$lines.Add("# $script:count UI Automation elements")
$lines -join "`r`n"
