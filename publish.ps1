$ErrorActionPreference = "Stop"

# Load .env variables
if (Test-Path .env) {
    Get-Content .env | Where-Object { $_ -match '=' -and $_ -notmatch '^#' } | ForEach-Object { 
        $k, $v = $_.Split('=', 2); 
        # Clean the value of surrounding quotes or trailing comments
        $v = $v.Split('#')[0].Trim().Trim('"').Trim("'")
        Set-Item env:$k $v 
    }
}

# Bump the version 
# & ".\bump-version.ps1"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Read version from file
$VersionFilePath = "VERSION.txt"
if (-not (Test-Path $VersionFilePath)) { throw "VERSION.txt not found." }
$AppVersion = (Get-Content -Path $VersionFilePath -Raw).Trim()


# Build 
# & ".\docker-build-initializer.ps1"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
# & ".\docker-build-webapi.ps1"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& ".\docker-build-webapp.ps1"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Push to registry 
if ($env:DOCKER_REGISTRY) {
    if (-not $env:DOCKER_REGISTRY.EndsWith("/")) { $env:DOCKER_REGISTRY += "/" }

    Write-Host "Pushing images to registry..." -ForegroundColor Cyan

    docker push "$($env:DOCKER_REGISTRY)squarebuddy-webapp:$AppVersion"
    docker push "$($env:DOCKER_REGISTRY)squarebuddy-webapp:latest"

   # docker push "$($env:DOCKER_REGISTRY)squarebuddy-webapi:$AppVersion"
   # docker push "$($env:DOCKER_REGISTRY)squarebuddy-webapi:latest"

   # docker push "$($env:DOCKER_REGISTRY)squarebuddy-initializer:$AppVersion"
   # docker push "$($env:DOCKER_REGISTRY)squarebuddy-initializer:latest"

} else {
    Write-Host "Skipping push (DOCKER_REGISTRY not defined)." -ForegroundColor Yellow
    return 1
}

# Update version on Coolify vars and Trigger service restart

if ($env:COOLIFY_API_TOKEN -and $env:COOLIFY_BASE_URL -and $env:COOLIFY_SERVICE_UUID) {
    $BaseUrl = $env:COOLIFY_BASE_URL.Trim().TrimEnd('/')
    $Token   = $env:COOLIFY_API_TOKEN.Trim()
    $Uuid    = $env:COOLIFY_SERVICE_UUID.Trim()

    Write-Host "Updating Coolify service (IMAGE_TAG) to: $AppVersion" -ForegroundColor Cyan
    
    $headers = @{
        "Authorization" = "Bearer $Token"
        "Content-Type"  = "application/json"
        "Accept"        = "application/json"
    }

    # Based on docs: key, value, and optional boolean flags
    $body = @{
        "key"           = "IMAGE_TAG"
        "value"         = "$AppVersion"
        "is_preview"    = $false
        "is_literal"    = $true
        "is_multiline"  = $false
        "is_shown_once" = $false
    } | ConvertTo-Json -Compress

    try {
        $EnvUri = "$BaseUrl/services/$Uuid/envs"
        $RestartUri = "$BaseUrl/services/$Uuid/restart"

        # 1. Update the variable using PATCH (as specified in docs for updates)
        Write-Host "Sending PATCH request to: $EnvUri" -ForegroundColor Gray
        Invoke-RestMethod -Method Patch -Uri $EnvUri -Headers $headers -Body $body

        # 2. Trigger Redeployment
        Write-Host "Restarting service to apply changes..." -ForegroundColor Cyan
        $Response = Invoke-RestMethod -Method Get -Uri $RestartUri -Headers $headers
        
        if ($Response.message) {
            Write-Host "Coolify Response: $($Response.message)" -ForegroundColor Green
        }
        Write-Host "Successfully signaled Coolify to restart the service!" -ForegroundColor Green
    }
    catch {
        $msg = $_.Exception.Message
        # Catching the body of the error if available for better debugging
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $msg = $reader.ReadToEnd()
        }
        Write-Host "Coolify API Error: $msg" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Skipping Coolify update (Required variables missing)." -ForegroundColor Yellow
}