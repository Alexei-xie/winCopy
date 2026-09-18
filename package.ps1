param([string]$IsccPath = '')
$ErrorActionPreference = 'Stop'
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must use major.minor.patch.' }
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory dist
$dist = Join-Path $PSScriptRoot 'dist'
$test = Start-Process -FilePath (Join-Path $dist 'winCopy.exe') -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content -LiteralPath (Join-Path $dist 'self-test-result.txt')
if ($test.ExitCode -ne 0) { throw 'Self-test failed.' }
if (-not $IsccPath) {
    $IsccPath = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) { throw 'Install Inno Setup 6 or pass -IsccPath.' }
& $IsccPath "/DAppVersion=$version" (Join-Path $PSScriptRoot 'installer\winCopy.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$zip = Join-Path $artifacts "winCopy-$version-portable.zip"
Compress-Archive -LiteralPath (Join-Path $dist 'winCopy.exe'), (Join-Path $PSScriptRoot 'README.md'), (Join-Path $PSScriptRoot 'LICENSE') -DestinationPath $zip -Force
$files = @((Join-Path $artifacts "winCopy-$version-setup.exe"), $zip)
$checksums = $files | ForEach-Object { $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256; '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), [IO.Path]::GetFileName($_) }
$checksums | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ascii
