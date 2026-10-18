<#
.SYNOPSIS
    React Native Windows (RNW 0.83) native toolchain preflight & installer.
.DESCRIPTION
    `npm run windows` (react-native run-windows) requires a Visual Studio instance
    >= 18.6.0 that contains BOTH Microsoft.Component.MSBuild and the VC Tools for
    the target architecture (see @react-native-windows/cli msbuildtools.js
    findAvailableVersion). This script runs the identical vswhere query so the
    result matches what the RNW CLI sees, and can add the missing workloads to an
    existing VS 2026 instance (self-elevates via UAC).

    Rules: R-01 (React Native CLI is the primary runner), AGENTS.md Section 5.
.EXAMPLE
    .\scripts\setup-windows-toolchain.ps1            # check only (default, no changes)
    .\scripts\setup-windows-toolchain.ps1 install    # add missing VS components (UAC prompt)
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("check", "install")]
    [string]$Command = "check",

    [ValidateSet("x64", "ARM64")]
    [string]$Arch = "x64"
)

$ErrorActionPreference = "Stop"

# Keep in sync with node_modules/@react-native-windows/cli/lib-commonjs/utils/msbuildtools.js
$MinVsVersion = "18.6.0"
$VcTools = if ($Arch -eq "ARM64") { "Microsoft.VisualStudio.Component.VC.Tools.ARM64" } else { "Microsoft.VisualStudio.Component.VC.Tools.x86.x64" }

# What the RNW CLI strictly requires to locate MSBuild.
$CliRequired = @("Microsoft.Component.MSBuild", $VcTools)

# Full set installed by rnw-dependencies.ps1 (appDev) so builds and the VS IDE both work.
$Components = @(
    "Microsoft.Component.MSBuild",
    $VcTools,
    "Microsoft.VisualStudio.ComponentGroup.UWP.Support",
    "Microsoft.VisualStudio.ComponentGroup.NativeDesktop.Core",
    "Microsoft.VisualStudio.Component.Windows11SDK.22621",
    "Microsoft.VisualStudio.ComponentGroup.UWP.VC"
)
$Workloads = @(
    "Microsoft.VisualStudio.Workload.ManagedDesktop",
    "Microsoft.VisualStudio.Workload.NativeDesktop",
    "Microsoft.VisualStudio.Workload.Universal"
)

$InstallerDir = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
$VsWhere = Join-Path $InstallerDir "vswhere.exe"
$VsSetup = Join-Path $InstallerDir "setup.exe"

function Write-Status([string]$Label, [bool]$Ok, [string]$Detail = "") {
    $tag = if ($Ok) { "[ OK ]" } else { "[FAIL]" }
    $color = if ($Ok) { "Green" } else { "Red" }
    Write-Host ("{0} {1,-58} {2}" -f $tag, $Label, $Detail) -ForegroundColor $color
}

function Get-VsInstances {
    $json = & $VsWhere -all -prerelease -products * -format json -utf8
    if (-not $json) { return }
    # Windows PowerShell 5.1 emits a JSON array as ONE pipeline object; unroll it explicitly.
    $parsed = ($json -join "`n") | ConvertFrom-Json
    # Only real Visual Studio products (excludes SSMS etc., which share the VS installer).
    foreach ($item in $parsed) { if ($item.channelId -like "VisualStudio.*") { $item } }
}

function Test-InstanceHas([string]$InstallPath, [string[]]$Requires) {
    $hits = & $VsWhere -all -prerelease -products * -requires $Requires -property installationPath
    return @($hits) -contains $InstallPath
}

if (-not (Test-Path $VsWhere)) {
    Write-Status "Visual Studio Installer (vswhere.exe)" $false "not found"
    Write-Host "Install Visual Studio 2026 Community: https://visualstudio.microsoft.com/downloads/" -ForegroundColor Yellow
    exit 1
}

# 1. Exact RNW CLI query: VS >= 18.6 with MSBuild + VC Tools.
$range = "[$MinVsVersion,19.0)"
$ready = @(& $VsWhere -all -prerelease -products * -version $range -requires $CliRequired -property installationPath)

Write-Host "`n== React Native Windows toolchain preflight (min VS $MinVsVersion, arch $Arch) ==" -ForegroundColor Cyan
foreach ($vs in (Get-VsInstances | Sort-Object { [version]$_.installationVersion } -Descending)) {
    if ($vs.productId -notlike "Microsoft.VisualStudio.Product.*") { continue }
    $versionOk = [version]$vs.installationVersion -ge [version]$MinVsVersion
    $vcOk = Test-InstanceHas $vs.installationPath @($VcTools)
    $detail = "v$($vs.installationVersion)  VCTools=$(if ($vcOk) {'yes'} else {'NO'})  $($vs.installationPath)"
    Write-Status $vs.displayName ($versionOk -and $vcOk) $detail
}

$devKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"
$devProp = Get-ItemProperty -Path $devKey -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
$devOk = ($devProp -ne $null -and $devProp.AllowDevelopmentWithoutDevLicense -eq 1)
$devDetail = if ($devOk) { "AllowDevelopmentWithoutDevLicense=1" } else { "DISABLED or unconfigured (Enable in Settings -> System -> For developers)" }
Write-Status "Windows Developer Mode" $devOk $devDetail

if ($ready.Count -gt 0) {
    Write-Host "`nREADY: RNW CLI will use MSBuild from $($ready[0])" -ForegroundColor Green
    Write-Host "Next: cd src/Pipeline.Presentation; npm run windows"
    exit 0
}

# 2. Pick the newest VS instance that satisfies the version but lacks components.
$target = Get-VsInstances |
    Where-Object { $_.productId -like "Microsoft.VisualStudio.Product.*" -and [version]$_.installationVersion -ge [version]$MinVsVersion } |
    Sort-Object { [version]$_.installationVersion } -Descending |
    Select-Object -First 1

if (-not $target) {
    Write-Host "`nNo Visual Studio >= $MinVsVersion found. Update an existing VS 2026 instance in the Visual Studio Installer, or run:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File src\Pipeline.Presentation\node_modules\react-native-windows\scripts\rnw-dependencies.ps1 -Install -Check VSUWP"
    exit 1
}

$missing = @(($Components + $Workloads) | Where-Object { -not (Test-InstanceHas $target.installationPath @($_)) })
Write-Host "`nNOT READY: '$($target.displayName)' ($($target.installationPath)) is missing:" -ForegroundColor Yellow
$missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }

if ($Command -ne "install") {
    Write-Host "`nFix (UAC prompt, multi-GB download):" -ForegroundColor Cyan
    Write-Host "  from src/Pipeline.Presentation:  npm run setup:windows" -ForegroundColor Cyan
    Write-Host "  from repo root:                  .\scripts\setup-windows-toolchain.ps1 install" -ForegroundColor Cyan
    exit 1
}

# 3. Install: self-elevate if needed, then modify the target instance in place.
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Requesting elevation..." -ForegroundColor Cyan
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" install -Arch $Arch"
    try {
        $proc = Start-Process -FilePath "powershell.exe" -ArgumentList $argList -Verb RunAs -Wait -PassThru
        exit $proc.ExitCode
    }
    catch {
        Write-Host "Could not request elevation ($($_.Exception.Message))." -ForegroundColor Red
        Write-Host "Do ONE of the following manually:" -ForegroundColor Yellow
        Write-Host "  A) Open 'Windows PowerShell' as Administrator and run:" -ForegroundColor Yellow
        Write-Host "       & `"$PSCommandPath`" install" -ForegroundColor Yellow
        Write-Host "  B) Visual Studio Installer > '$($target.displayName)' > Modify > tick" -ForegroundColor Yellow
        Write-Host "     'Desktop development with C++', 'WinUI application development'" -ForegroundColor Yellow
        Write-Host "     and '.NET desktop development' > Modify." -ForegroundColor Yellow
        exit 1
    }
}

$installArgs = @("modify", "--installPath", "`"$($target.installationPath)`"", "--passive", "--norestart", "--includeRecommended")
foreach ($id in ($Components + $Workloads)) { $installArgs += @("--add", $id) }

Write-Host "Running: setup.exe $($installArgs -join ' ')" -ForegroundColor Cyan
$p = Start-Process -FilePath $VsSetup -ArgumentList $installArgs -Wait -PassThru
# 3010 = success, reboot required
if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
    Write-Host "Visual Studio Installer exited with code $($p.ExitCode)." -ForegroundColor Red
    exit $p.ExitCode
}
if ($p.ExitCode -eq 3010) { Write-Host "Installed. A reboot is required before building." -ForegroundColor Yellow }

$ready = @(& $VsWhere -all -prerelease -products * -version $range -requires $CliRequired -property installationPath)
if ($ready.Count -gt 0) {
    Write-Host "READY: RNW CLI will use MSBuild from $($ready[0])" -ForegroundColor Green
    exit 0
}
Write-Host "Components still not detected; open the Visual Studio Installer to inspect." -ForegroundColor Red
exit 1
