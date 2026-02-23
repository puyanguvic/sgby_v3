param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
    throw "Zip not found: $ZipPath"
}

$work = Join-Path $env:RUNNER_TEMP ("ibaye-smoke-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null
Expand-Archive -Path $ZipPath -DestinationPath $work -Force

$exe = Join-Path $work "iBaye.exe"
$runBat = Join-Path $work "Run_With_Log.bat"
$probePs1 = Join-Path $work "RuntimeProbe.ps1"
$logDir = Join-Path $work "logs"
$ibayeLog = Join-Path $logDir "ibaye_startup.log"
$hostTrace = Join-Path $logDir "dotnet_host_trace.log"

foreach ($f in @($exe, $runBat, $probePs1)) {
    if (-not (Test-Path -LiteralPath $f -PathType Leaf)) {
        throw "Required file missing in package: $f"
    }
}

Write-Host "== Runtime probe =="
powershell -NoProfile -ExecutionPolicy Bypass -File $probePs1

New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$cmd = "set COREHOST_TRACE=1 && set COREHOST_TRACEFILE=""$hostTrace"" && ""$exe"" --verbose --log-file ""$ibayeLog"""

Write-Host "== Start iBaye smoke process =="
$p = Start-Process -FilePath "cmd.exe" -ArgumentList "/c", $cmd -PassThru

$timeoutSec = 20
if (-not $p.WaitForExit($timeoutSec * 1000)) {
    Write-Host "Process still running after $timeoutSec sec, killing for smoke timeout."
    $p.Kill()
}

if (-not (Test-Path -LiteralPath $ibayeLog -PathType Leaf)) {
    throw "ibaye_startup.log not generated."
}

$logText = Get-Content -LiteralPath $ibayeLog -Raw

$fatalPatterns = @(
    "No loader found for resource: res://scripts/.+\.cs",
    "Failed loading scene: res://scenes/main/Main\.tscn"
)
foreach ($pat in $fatalPatterns) {
    if ($logText -match $pat) {
        throw "Smoke failed: matched fatal pattern '$pat'"
    }
}

Write-Host "== Smoke summary =="
Write-Host "Process exit code: $($p.ExitCode)"
Write-Host "Log path: $ibayeLog"
Write-Host "Host trace path: $hostTrace"

