# Downloads a MaIN.Docs generated artifact, extracts it, and runs it with `dotnet run`.
# Usage: & ([scriptblock]::Create((irm <host>/scripts/run-artifact.ps1))) -ArtifactUrl "<artifactUrl>" -ArchiveName "<archiveName>"
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactUrl,

    [string]$ArchiveName = "artifact.zip"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host ""
Write-Host "  ╔═══════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║      " -NoNewline -ForegroundColor Cyan
Write-Host "MaIN " -NoNewline -ForegroundColor Magenta
Write-Host "Package Runner" -NoNewline -ForegroundColor Cyan
Write-Host "      ║" -ForegroundColor Cyan
Write-Host "  ╚═══════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$dir = [System.IO.Path]::GetFileNameWithoutExtension($ArchiveName)

Write-Host "==> Downloading $ArchiveName" -ForegroundColor Yellow
Invoke-WebRequest -Uri $ArtifactUrl -OutFile $ArchiveName

Write-Host "==> Extracting to .\$dir" -ForegroundColor Yellow
if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
Expand-Archive -Path $ArchiveName -DestinationPath $dir -Force

$projectFile = Get-ChildItem -Path $dir -Filter *.csproj -Recurse | Select-Object -First 1
if (-not $projectFile) {
    Write-Error "No .csproj found in $dir"
    exit 1
}
$projectDir = $projectFile.DirectoryName

Write-Host "==> Running 'dotnet run' in $projectDir" -ForegroundColor Green
Write-Host ""
Set-Location $projectDir
dotnet run
