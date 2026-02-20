param(
    [string]$BuildDir = "build-win",
    [string]$DistDir = "dist",
    [string]$Config = "Release",
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Add-Msys2ToPath {
    $candidates = @()
    if ($env:MSYS2_ROOT) {
        $candidates += $env:MSYS2_ROOT
    }
    $candidates += "C:\msys64"
    $candidates += "D:\msys64"

    foreach ($root in $candidates) {
        if (-not (Test-Path $root)) {
            continue
        }
        $ucrtBin = Join-Path $root "ucrt64\bin"
        $usrBin = Join-Path $root "usr\bin"
        $gcc = Join-Path $ucrtBin "gcc.exe"
        if (Test-Path $gcc) {
            $newPath = @()
            if (Test-Path $ucrtBin) { $newPath += $ucrtBin }
            if (Test-Path $usrBin) { $newPath += $usrBin }
            $newPath += $env:PATH
            $env:PATH = ($newPath -join ";")
            return $root
        }
    }

    return $null
}

function Require-Command([string]$name, [string]$hint) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Missing command: $name. $hint"
    }
}

function Find-Iscc {
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($item in $candidates) {
        if (Test-Path $item) {
            return $item
        }
    }
    return $null
}

function Ensure-Dir([string]$path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

Write-Host "== iBaye Windows installer build =="
Write-Host "Repo: $repoRoot"

$msysRoot = Add-Msys2ToPath
if ($msysRoot) {
    Write-Host "Using MSYS2 toolchain from: $msysRoot"
}

Require-Command "cmake" "Install MSYS2 UCRT64 cmake package."
Require-Command "ninja" "Install MSYS2 UCRT64 ninja package."
Require-Command "gcc" "Install MSYS2 UCRT64 GCC toolchain package."

$iscc = Find-Iscc
if (-not $iscc) {
    throw "Cannot find ISCC.exe. Install Inno Setup 6 or add ISCC.exe to PATH."
}

if ($Clean) {
    if (Test-Path $BuildDir) { Remove-Item $BuildDir -Recurse -Force }
    if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
    if (Test-Path "installer") { Remove-Item "installer" -Recurse -Force }
}

Ensure-Dir $BuildDir

Write-Host "Configuring CMake..."
& cmake -S . -B $BuildDir -G Ninja -DCMAKE_BUILD_TYPE=$Config -DCMAKE_C_COMPILER=gcc

Write-Host "Building..."
& cmake --build $BuildDir -j

$exePath = Join-Path $BuildDir "src\baye.exe"
if (-not (Test-Path $exePath)) {
    throw "Build finished but executable not found: $exePath"
}

Ensure-Dir $DistDir
Copy-Item $exePath (Join-Path $DistDir "baye.exe") -Force

$fontBin = "src\font.bin"
if (-not (Test-Path $fontBin)) {
    throw "Missing required file: $fontBin"
}
Copy-Item $fontBin (Join-Path $DistDir "font.bin") -Force

if (Test-Path "src\dat.lib") {
    Copy-Item "src\dat.lib" (Join-Path $DistDir "dat.lib") -Force
} elseif (Test-Path "src\dat.lib.orig") {
    Copy-Item "src\dat.lib.orig" (Join-Path $DistDir "dat.lib") -Force
} else {
    throw "Missing required resource: src\dat.lib or src\dat.lib.orig"
}

Get-ChildItem -Path "src" -Filter "font24.cn.*" -File -ErrorAction SilentlyContinue |
    Copy-Item -Destination $DistDir -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path "src" -Filter "font24.en.*" -File -ErrorAction SilentlyContinue |
    Copy-Item -Destination $DistDir -Force -ErrorAction SilentlyContinue

$dllNames = @(
    "libwinpthread-1.dll",
    "libgcc_s_seh-1.dll",
    "libstdc++-6.dll"
)

$msysBinCandidates = @(
    "C:\msys64\ucrt64\bin",
    "D:\msys64\ucrt64\bin"
)
if ($env:MSYS2_ROOT) {
    $msysBinCandidates = @((Join-Path $env:MSYS2_ROOT "ucrt64\bin")) + $msysBinCandidates
}

foreach ($dll in $dllNames) {
    foreach ($bin in $msysBinCandidates) {
        $src = Join-Path $bin $dll
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $DistDir $dll) -Force
            break
        }
    }
}

Write-Host "Building installer with Inno Setup..."
& $iscc "installer.iss"

$installerPath = Join-Path $repoRoot "installer\iBaye-Setup.exe"
if (Test-Path $installerPath) {
    Write-Host ""
    Write-Host "Done."
    Write-Host "Installer: $installerPath"
} else {
    Write-Host ""
    Write-Host "Build finished, but installer path not found at:"
    Write-Host $installerPath
    Write-Host "Check Inno Setup output above."
}
