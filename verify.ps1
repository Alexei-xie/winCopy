param([string]$OutputDirectory = "dist")
$ErrorActionPreference = 'Stop'
$exe = Join-Path (Join-Path $PSScriptRoot $OutputDirectory) 'winCopy.exe'
$result = Start-Process -FilePath $exe -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content -LiteralPath (Join-Path (Join-Path $PSScriptRoot $OutputDirectory) 'self-test-result.txt')
if ($result.ExitCode -ne 0) { throw 'Self tests failed.' }
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$assembly = [Reflection.Assembly]::LoadFrom($exe)
[System.Windows.Forms.Application]::EnableVisualStyles()
$main = New-Object WinCopy.MainWindow($false)
try {
    $flags = [Reflection.BindingFlags]"Instance,NonPublic"; $main.GetType().GetField("storageBlocked", $flags).SetValue($main, $true); $demo = New-Object WinCopy.Database; $demo.Hotkey = "J"; foreach ($text in @("Design notes - winCopy", "https://github.com/Clipy/Clipy", "A little less searching. A little more creating.", "Meeting notes")) { $clip = New-Object WinCopy.Clip; $clip.Text = $text; $clip.Source = "Demo"; $demo.Items.Add($clip) }; $main.GetType().GetField("db", $flags).SetValue($main, $demo); $main.GetType().GetMethod("RefreshItems", $flags).Invoke($main, @()); $main.Show(); [System.Windows.Forms.Application]::DoEvents()
    if ($main.Width -ne 480 -or $main.FormBorderStyle -ne "None" -or $main.ShowInTaskbar) { throw "Popup geometry regression" }
    $bitmap = New-Object System.Drawing.Bitmap($main.Width, $main.Height)
    $main.DrawToBitmap($bitmap, (New-Object System.Drawing.Rectangle(0, 0, $main.Width, $main.Height)))
    $bitmap.Save((Join-Path (Join-Path $PSScriptRoot $OutputDirectory) 'preview.png'))
    $bitmap.Dispose()
    $database = New-Object WinCopy.Database
    $settings = New-Object WinCopy.SettingsDialog($database)
    try {
        $settings.Show(); [System.Windows.Forms.Application]::DoEvents()
        $bitmap = New-Object System.Drawing.Bitmap($settings.Width, $settings.Height)
        $settings.DrawToBitmap($bitmap, (New-Object System.Drawing.Rectangle(0, 0, $settings.Width, $settings.Height)))
        $bitmap.Save((Join-Path (Join-Path $PSScriptRoot $OutputDirectory) 'settings-preview.png'))
        $bitmap.Dispose()
    } finally { $settings.Dispose() }
    Write-Output 'PASS: main window and settings construction / render.'
} finally {
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    $main.GetType().GetField('quitting', $flags).SetValue($main, $true)
    $main.Close()
    $main.Dispose()
}
