# Downloads a MaIN.Docs generated artifact, extracts it, and runs it with `dotnet run`.
# Usage: & ([scriptblock]::Create((irm <host>/scripts/run-artifact.ps1))) -ArtifactUrl "<artifactUrl>" -ArchiveName "<archiveName>"
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactUrl,

    [string]$ArchiveName = "artifact.zip"
)

$ProgressPreference = "SilentlyContinue"

function Copy-ToClipboard([string]$Text) {
    try { Set-Clipboard -Value $Text; return $true } catch { return $false }
}

function Fail-Build([string]$Output) {
    Write-Host ""
    Write-Host "  ✗ Build failed" -ForegroundColor Red
    Write-Host ""
    $Output -split "`n" | Where-Object { $_ -match "\): error " } | Select-Object -First 25 | ForEach-Object { Write-Host $_ }
    Write-Host ""
    $msg = "The generated artifact failed to build. Please fix all the files:`n`n$Output"
    if (Copy-ToClipboard $msg) {
        Write-Host "  ✓ Build errors copied to clipboard — paste into the chat to get it fixed." -ForegroundColor Cyan
    } else {
        Write-Host "  → Copy the errors above and paste into the MaIN.Docs chat to get them fixed." -ForegroundColor Yellow
    }
    Write-Host ""
    exit 1
}

function Fail-Run([int]$Code, [string]$Output) {
    Write-Host ""
    Write-Host "  ✗ Process exited with code $Code" -ForegroundColor Red
    Write-Host ""
    $lines = ($Output -split "`n" | Select-Object -Last 60) -join "`n"
    $msg = "The generated artifact crashed at runtime (exit code $Code). Please fix the code:`n`n$lines"
    if (Copy-ToClipboard $msg) {
        Write-Host "  ✓ Error output copied to clipboard — paste into the chat to get it fixed." -ForegroundColor Cyan
    } else {
        Write-Host "  → Copy the output above and paste into the MaIN.Docs chat to get it fixed." -ForegroundColor Yellow
    }
    Write-Host ""
}

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
try {
    Invoke-WebRequest -Uri $ArtifactUrl -OutFile $ArchiveName
} catch {
    Write-Host "Download failed: $_" -ForegroundColor Red; exit 1
}

Write-Host "==> Extracting to .\$dir" -ForegroundColor Yellow
if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
Expand-Archive -Path $ArchiveName -DestinationPath $dir -Force

$projectFile = Get-ChildItem -Path $dir -Filter *.csproj -Recurse | Select-Object -First 1
if (-not $projectFile) { Write-Host "No .csproj found in $dir" -ForegroundColor Red; exit 1 }
$projectDir = $projectFile.DirectoryName
Push-Location $projectDir

# ── Build ─────────────────────────────────────────────────────────────────
Write-Host "==> Building project..." -ForegroundColor Yellow
$buildOut = dotnet build 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) { Fail-Build $buildOut }
Write-Host "  ✓ Build succeeded" -ForegroundColor Green

# ── Run ───────────────────────────────────────────────────────────────────
Write-Host "==> Running..." -ForegroundColor Green
Write-Host ""

$runLog = [System.IO.Path]::GetTempFileName()
# Tee to file so we can capture output for clipboard if the process crashes.
# Interactive stdin still works since only stdout/stderr are redirected here;
# dotnet's own Console.ReadLine() reads from the parent terminal.
$proc = Start-Process dotnet -ArgumentList "run","--no-build" `
    -WorkingDirectory $projectDir -NoNewWindow -PassThru `
    -RedirectStandardOutput $runLog -RedirectStandardError "$runLog.err"
$proc.WaitForExit()

# Stream captured output to terminal (best-effort for non-interactive apps)
if (Test-Path $runLog)     { Get-Content $runLog     }
if (Test-Path "$runLog.err") { Get-Content "$runLog.err" | Write-Host -ForegroundColor Red }

# 0 or -1073741510 (Ctrl+C on Windows) are clean exits
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne -1073741510) {
    $captured = (Get-Content $runLog -Raw) + (Get-Content "$runLog.err" -Raw -ErrorAction SilentlyContinue)
    Fail-Run $proc.ExitCode $captured
}
Remove-Item $runLog, "$runLog.err" -ErrorAction SilentlyContinue
Pop-Location
