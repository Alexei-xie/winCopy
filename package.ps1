param([string]$IsccPath = '')
$ErrorActionPreference = 'Stop'
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must use major.minor.patch.' }
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory dist
$dist = Join-Path $PSScriptRoot 'dist'
$test = Start-Process -FilePath (Join-Path $dist 'winCopy.exe') -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content -LiteralPath (Join-Path $dist 'self-test-result.txt')
if ($test.ExitCode -ne 0) { throw 'Self-test failed.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $compiler /nologo /target:exe /codepage:65001 "/out:$dist\UpdateRegression.exe" "/reference:$dist\winCopy.exe" (Join-Path $PSScriptRoot 'tests\UpdateRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Update test compilation failed.' }
& (Join-Path $dist 'UpdateRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Update regression tests failed.' }
& $compiler /nologo /target:exe /codepage:65001 "/out:$dist\StorageRegression.exe" "/reference:$dist\winCopy.exe" /reference:System.Core.dll (Join-Path $PSScriptRoot 'tests\StorageRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Storage test compilation failed.' }
& (Join-Path $dist 'StorageRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Storage regression tests failed.' }
& $compiler /nologo /target:exe /codepage:65001 "/out:$dist\GroupRegression.exe" "/reference:$dist\winCopy.exe" /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'tests\GroupRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Group test compilation failed.' }
& (Join-Path $dist 'GroupRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Group regression tests failed.' }
& $compiler /nologo /target:exe /codepage:65001 "/out:$dist\NumberRegression.exe" "/reference:$dist\winCopy.exe" /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'tests\NumberRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Numeric test compilation failed.' }
& (Join-Path $dist 'NumberRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Numeric regression tests failed.' }
& $compiler /nologo /target:exe /codepage:65001 "/out:$dist\MenuRegression.exe" "/reference:$dist\winCopy.exe" /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'tests\MenuRegression.cs')
if ($LASTEXITCODE -ne 0) { throw 'Menu test compilation failed.' }
& (Join-Path $dist 'MenuRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Menu regression tests failed.' }
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
