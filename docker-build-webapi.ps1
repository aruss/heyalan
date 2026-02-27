# build-web.ps1
$ErrorActionPreference = "Stop"

# Read version from file
$VersionFilePath = "VERSION.txt"
if (-not (Test-Path $VersionFilePath)) { throw "VERSION.txt not found." }
$AppVersion = (Get-Content -Path $VersionFilePath -Raw).Trim()

# Load .env variables
if (Test-Path .env) {
    Get-Content .env | Where-Object { $_ -match '=' -and $_ -notmatch '^#' } | ForEach-Object { 
        $k, $v = $_.Split('=', 2); 
        # Clean the value of surrounding quotes or trailing comments
        $v = $v.Split('#')[0].Trim().Trim('"').Trim("'")
        Set-Item env:$k $v 
    }
}

# Context Staging
$TempDir = Join-Path $env:TEMP "ShelfBuddy_WebBuild_$(Get-Random)"
New-Item -ItemType Directory -Path $TempDir -Force

try {
    Write-Host "Staging projects to $TempDir..." -ForegroundColor Cyan
    $IgnorePathes = 'bin', 'obj', 'node_modules', '.git', '.vs', '.vscode'

    function Copy-Clean ($Source, $Dest, $Blacklist) {
        # Resolve to full path to avoid issues with relative paths
        $Source = (Resolve-Path $Source).Path
        $Items = Get-ChildItem -Path $Source -Recurse

        foreach ($Item in $Items) {
            # Check if the path contains any word from the blacklist
            $Skip = $false
            foreach ($Pattern in $Blacklist) {
                if ($Item.FullName -like "*$Pattern*") { $Skip = $true; break }
            }

            if (-not $Skip) {
                # Map the source path to the destination path
                $RelativePath = $Item.FullName.Substring($Source.Length)
                $NewPath = Join-Path $Dest $RelativePath

                if ($Item.PSIsContainer) {
                    New-Item -Path $NewPath -ItemType Directory -Force | Out-Null
                } else {
                    # Ensure the folder exists before copying the file
                    $Parent = Split-Path $NewPath
                    if (-not (Test-Path $Parent)) { New-Item $Parent -ItemType Directory -Force | Out-Null }
                    Copy-Item $Item.FullName -Destination $NewPath -Force
                }
            }
        }
    }

    Copy-Clean "./ShelfBuddy" (Join-Path $TempDir "ShelfBuddy") $IgnorePathes
    Copy-Clean "./ShelfBuddy.WebApi" (Join-Path $TempDir "ShelfBuddy.WebApi") $IgnorePathes

    # Diagnostic: Print the actual size of the context before building
    $SizeMB = [Math]::Round(((Get-ChildItem $TempDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
    Write-Host "Context Size: $SizeMB MB | Version: $AppVersion" -ForegroundColor Yellow
    
    # Sanitize DOCKER_REGISTRY (Ensure trailing slash if set, else empty)
    if ($env:DOCKER_REGISTRY) {
        if (-not $env:DOCKER_REGISTRY.EndsWith("/")) { $env:DOCKER_REGISTRY += "/" }
    } else {
        $env:DOCKER_REGISTRY = ""
    }

    $ImageName = "shelfbuddy-webapi"
    Write-Host "Building ${ImageName}:$AppVersion (Registry: '$env:DOCKER_REGISTRY')" -ForegroundColor Cyan

    docker build --no-cache `
        --build-arg "APP_VERSION=$AppVersion" `
        -t "$($env:DOCKER_REGISTRY)${ImageName}:$AppVersion" `
        -t "$($env:DOCKER_REGISTRY)${ImageName}:latest" `
        -f "ShelfBuddy.WebApi/Dockerfile" `
        $TempDir
}
finally {
    Write-Host "Cleaning up..." -ForegroundColor Gray
    Remove-Item -Path $TempDir -Recurse -Force # Uncomment after verifying size
}