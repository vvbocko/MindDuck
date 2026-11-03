# Builds MindDuck.Contracts and copies it into the Unity project:
#   MindDuck.Contracts.dll  - the compiled classes
#   MindDuck.Contracts.xml  - the /// comments (tooltips in the editor)
#   link.xml                - keeps the classes in WebGL builds (original lives next to the Contracts source)
#
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File helpers\copy-contracts.ps1
# Needs the .NET 10 SDK (https://dotnet.microsoft.com/download).

$ErrorActionPreference = 'Stop'

$root     = Split-Path -Parent $PSScriptRoot
$source   = Join-Path $root 'MindDuckServer\src\MindDuck.Contracts'
$project  = Join-Path $source 'MindDuck.Contracts.csproj'
$output   = Join-Path $source 'bin\Release\netstandard2.1'
$target   = Join-Path $root 'MindDuckClient\Assets\Plugins\MindDuck'

Write-Host "Building MindDuck.Contracts..."
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed - nothing was copied." }

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $output 'MindDuck.Contracts.dll') $target -Force
Copy-Item (Join-Path $output 'MindDuck.Contracts.xml') $target -Force
Copy-Item (Join-Path $source 'link.xml') $target -Force

Write-Host "Copied MindDuck.Contracts.dll, .xml and link.xml to $target"
Write-Host "Switch to Unity so it re-imports the DLL, then commit it together with your Contracts change."
