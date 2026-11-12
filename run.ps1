$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $root 'dist\MacKey.exe'
if (-not (Test-Path $exe)) { & (Join-Path $root 'build.ps1') }
Start-Process $exe
