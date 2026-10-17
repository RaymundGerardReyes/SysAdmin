<#
.SYNOPSIS
    PipelineSolution Decoupled Semantic Versioning & Git Release Orchestrator
.DESCRIPTION
    Manages independent Semantic Versioning across backend, api, and frontend.
.EXAMPLE
    .\scripts\orchestrate-version.ps1 status
    .\scripts\orchestrate-version.ps1 bump backend patch
    .\scripts\orchestrate-version.ps1 bump api minor
    .\scripts\orchestrate-version.ps1 bump frontend patch
    .\scripts\orchestrate-version.ps1 init-baseline
#>
[CmdletBinding()]
param(
    [Parameter(Position=0)]
    [ValidateSet("status", "bump", "detect", "init-baseline")]
    [string]$Command = "status",

    [Parameter(Position=1)]
    [ValidateSet("backend", "api", "frontend")]
    [string]$Component,

    [Parameter(Position=2)]
    [ValidateSet("patch", "minor", "major")]
    [string]$BumpType = "patch",

    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$ManifestPath = Join-Path $RepoRoot "version.manifest.json"

function Get-Manifest {
    Get-Content $ManifestPath -Raw | ConvertFrom-Json
}

function Save-Manifest($manifest) {
    $json = $manifest | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($ManifestPath, $json + "`n", [System.Text.Encoding]::UTF8)
}

function Calculate-Bump([string]$current, [string]$type) {
    $parts = $current.Split('.')
    [int]$major = [int]$parts[0]
    [int]$minor = [int]$parts[1]
    [int]$patch = [int]$parts[2]

    switch ($type) {
        "major" { return "$($major + 1).0.0" }
        "minor" { return "$major.$($minor + 1).0" }
        "patch" { return "$major.$minor.$($patch + 1)" }
    }
}

switch ($Command) {
    "status" {
        Write-Host "`n==============================================================================" -ForegroundColor Blue
        Write-Host " PipelineSolution Decoupled Semantic Versioning Status" -ForegroundColor Blue
        Write-Host "==============================================================================`n" -ForegroundColor Blue

        $manifest = Get-Manifest
        "{0,-12} | {1,-10} | {2,-16} | {3,-16} | {4,-10}" -f "Component", "Version", "Tag Prefix", "Latest Git Tag", "Commits Ahead"
        "-" * 80

        foreach ($comp in @("backend", "api", "frontend")) {
            $ver = $manifest.components.$comp.version
            $tagPrefix = $manifest.components.$comp.tagPrefix
            $allTags = git tag -l "$tagPrefix*" --sort=-v:refname 2>$null
            $latestTag = if ($allTags) { ($allTags -split "`r?`n")[0] } else { "[none]" }
            $ahead = if ($latestTag -ne "[none]") { (git rev-list "$latestTag..HEAD" --count 2>$null) } else { "-" }

            "{0,-12} | {1,-10} | {2,-16} | {3,-16} | {4,-10}" -f $comp, $ver, $tagPrefix, $latestTag, $ahead
        }
        Write-Host ""
    }

    "bump" {
        if (-not $Component) {
            Write-Error "Component required for bump: backend, api, or frontend"
        }

        if (-not $Force) {
            $dirty = git status --porcelain 2>$null
            if ($dirty) {
                Write-Error "Working directory has uncommitted changes. Commit or stash first, or pass -Force."
            }
        }

        $manifest = Get-Manifest
        $current = $manifest.components.$Component.version
        $tagPrefix = $manifest.components.$Component.tagPrefix
        $newVer = Calculate-Bump $current $BumpType
        $newTag = "$tagPrefix$newVer"

        Write-Host "Bumping $Component from $current to $newVer ($BumpType)..." -ForegroundColor Cyan

        # 1. Update manifest
        $manifest.components.$Component.version = $newVer
        Save-Manifest $manifest

        # 2. Update component files
        switch ($Component) {
            "backend" {
                $propsPath = Join-Path $RepoRoot "Directory.Build.props"
                $content = [System.IO.File]::ReadAllText($propsPath)
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, "<BackendVersion Condition=.*>[^<]+</BackendVersion>", "<BackendVersion Condition=""'`$(BackendVersion)' == ''"">$newVer</BackendVersion>")
                [System.IO.File]::WriteAllText($propsPath, $content, [System.Text.Encoding]::UTF8)
                git add $ManifestPath $propsPath
            }
            "api" {
                $apiVerPath = Join-Path $RepoRoot "src\Pipeline.LocalApi\version.json"
                $apiData = Get-Content $apiVerPath -Raw | ConvertFrom-Json
                $apiData.version = $newVer
                $apiData.openApiSpecVersion = $newVer
                $apiData.releaseDate = (Get-Date).ToString("yyyy-MM-dd")
                $apiJson = $apiData | ConvertTo-Json -Depth 5
                [System.IO.File]::WriteAllText($apiVerPath, $apiJson + "`n", [System.Text.Encoding]::UTF8)

                $csprojPath = Join-Path $RepoRoot "src\Pipeline.LocalApi\Pipeline.LocalApi.csproj"
                $content = [System.IO.File]::ReadAllText($csprojPath)
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, "<ApiVersion Condition=.*>[^<]+</ApiVersion>", "<ApiVersion Condition=""'`$(ApiVersion)' == ''"">$newVer</ApiVersion>")
                [System.IO.File]::WriteAllText($csprojPath, $content, [System.Text.Encoding]::UTF8)

                git add $ManifestPath $apiVerPath $csprojPath
            }
            "frontend" {
                $pkgPath = Join-Path $RepoRoot "src\Pipeline.Presentation\package.json"
                $pkg = Get-Content $pkgPath -Raw | ConvertFrom-Json
                $pkg.version = $newVer
                $pkgJson = $pkg | ConvertTo-Json -Depth 10
                [System.IO.File]::WriteAllText($pkgPath, $pkgJson + "`n", [System.Text.Encoding]::UTF8)

                $buildPath = Join-Path $RepoRoot "src\Pipeline.Presentation\src\config\buildInfo.ts"
                $content = [System.IO.File]::ReadAllText($buildPath)
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, "version:\s*'[^']+'", "version: '$newVer'")
                $today = (Get-Date).ToString("yyyy-MM-dd")
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, "buildDate:\s*'[^']+'", "buildDate: '$today'")
                [System.IO.File]::WriteAllText($buildPath, $content, [System.Text.Encoding]::UTF8)

                git add $ManifestPath $pkgPath $buildPath
            }
        }

        # 3. Create commit & tag
        git commit -m "chore($Component): release $newTag"
        git tag -a $newTag -m "Release $newTag for $Component"
        Write-Host "Successfully bumped and tagged: $newTag" -ForegroundColor Green
        Write-Host "Push using: git push origin main && git push origin $newTag" -ForegroundColor Cyan
    }

    "detect" {
        Write-Host "`nConventional Commit Recommendation Detector" -ForegroundColor Cyan
        $manifest = Get-Manifest
        foreach ($comp in @("backend", "api", "frontend")) {
            $tagPrefix = $manifest.components.$comp.tagPrefix
            $allTags = git tag -l "$tagPrefix*" --sort=-v:refname 2>$null
            $latestTag = if ($allTags) { ($allTags -split "`r?`n")[0] } else { $null }

            $range = if ($latestTag) { "$latestTag..HEAD" } else { "HEAD" }
            $log = git log $range --format="%s" 2>$null

            $rec = "none"
            if ($log -match "BREAKING CHANGE" -or $log -match "$comp!:") {
                $rec = "major"
            } elseif ($log -match "feat\($comp\):") {
                $rec = "minor"
            } elseif ($log -match "fix\($comp\):" -or $log -match "perf\($comp\):") {
                $rec = "patch"
            }

            Write-Host "  $($comp): recommendation -> $rec" -ForegroundColor Green
        }
    }

    "init-baseline" {
        Write-Host "Reconciling baseline with origin/main..." -ForegroundColor Cyan
        $head = git rev-parse HEAD 2>$null
        if (-not $head) {
            $originMain = git rev-parse origin/main 2>$null
            if ($originMain) {
                git reset --mixed origin/main
            }
        }

        git add .
        git commit -m "feat(infra): initial architecture baseline with decoupled SemVer orchestration"

        $manifest = Get-Manifest
        foreach ($comp in @("backend", "api", "frontend")) {
            $ver = $manifest.components.$comp.version
            $tagPrefix = $manifest.components.$comp.tagPrefix
            $tagName = "$tagPrefix$ver"
            $exists = git rev-parse $tagName 2>$null
            if (-not $exists) {
                git tag -a $tagName -m "Initial baseline release for $comp v$ver"
                Write-Host "Tagged: $tagName" -ForegroundColor Green
            }
        }

        Write-Host "Pushing main and tags to origin..." -ForegroundColor Cyan
        git push -u origin main --tags
        Write-Host "Remote push complete!" -ForegroundColor Green
    }
}
