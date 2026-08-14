$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src\ScreenFloater\Program.cs'
$dist = Join-Path $root 'dist'
$out = Join-Path $dist 'ScreenFloater-x64.exe'

New-Item -ItemType Directory -Force -Path $dist | Out-Null

Add-Type `
  -Path $src `
  -ReferencedAssemblies 'System.Windows.Forms.dll','System.Drawing.dll' `
  -OutputAssembly $out `
  -OutputType WindowsApplication

Write-Host "Built $out"
