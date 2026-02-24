# docker-run.ps1
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

docker-compose -f docker-compose.local.yaml up