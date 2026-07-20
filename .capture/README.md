# UIA tree capture

Static proof that a Skia-rendered Uno Platform sample exposes a live UI Automation tree.

## Assets

| File | What it is |
| --- | --- |
| `uia-tree-accessibility.txt` | 70-element UIA tree of the Accessibility sample, with control types, AutomationIds, supported patterns, and live values |
| `app-accessibility-sample.png` | 1936x1168 capture of the same window at the same moment |

## Capture conditions

- App: `A11yCapture`, Accessibility sample, Uno Platform 6.6, `net10.0-desktop`, Skia renderer.
- Build: **Release**, launched as a bare exe. No devserver and no Hot Design attached, so no dev-tooling nodes appear in the tree.
- Reader: `System.Windows.Automation` (UIAutomationClient) walking `ControlViewWalker` from the top-level window down. An out-of-process UIA client, the same surface a screen reader consumes.

The tree is drivable, not only readable: `InvokePattern.Invoke()` on `NavAccessibility` moved the breadcrumb from `index` to `accessibility`.

## Reproduce

```powershell
dotnet build A11yCapture\A11yCapture.csproj -f net10.0-desktop -c Release
$p = Start-Process .\A11yCapture\bin\Release\net10.0-desktop\A11yCapture.exe -PassThru
Start-Sleep -Seconds 10

.\.capture\Dump-UiaTree.ps1  -ProcessId $p.Id > .capture\uia-tree-accessibility.txt
.\.capture\Capture-Window.ps1 -ProcessId $p.Id -OutPath .capture\app-accessibility-sample.png
```

`Capture-Window.ps1` uses `PrintWindow` with `PW_RENDERFULLCONTENT`. The window is GL-composited, so ordinary desktop-pixel grabs capture whatever occludes it.
