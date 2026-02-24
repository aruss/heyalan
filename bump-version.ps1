# 1. Check for uncommitted changes
if ((git status --porcelain) -ne $null) {
    Write-Host "Uncommitted changes detected. Aborting." -ForegroundColor Red
    exit 1
}

# 2. Check for changes since last tag
$LastTag = git describe --tags --abbrev=0 2>$null
if ($LastTag) {
    $CommitsSinceTag = git rev-list "${LastTag}..HEAD" --count
    $CommitsCount = [int]$CommitsSinceTag

    if ($CommitsCount -eq 0) {
        Write-Host "No commits since last tag ($LastTag). Current HEAD is at the tag." -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "Found $CommitsCount commits since $LastTag." -ForegroundColor Green
    }
} else {
    Write-Host "No tags found in the repository history." -ForegroundColor Cyan
}

# 3. Bump the version in VERSION.txt
$YearTwoDigit = Get-Date -Format "yy"      # e.g., 26
$MonthDay     = Get-Date -Format "MMdd"    # e.g., 0105
$TodayPrefix  = "$YearTwoDigit.$MonthDay"  # Result: 26.0105
$Path         = "VERSION.txt"
$Build        = 1

if (Test-Path $Path) {
    $Raw = (Get-Content -Path $Path -Raw).Trim()
    # Matches format like 26.0105.1
    if ($Raw -match "^(?<prefix>\d{2}\.\d{4})\.(?<build>\d+)$") {
        if ($Matches['prefix'] -eq $TodayPrefix) {
            $Build = [int]$Matches['build'] + 1
        }
    }
}

# Resulting format: 26.0105.1
$NewVersion = "${TodayPrefix}.${Build}"
Set-Content -Path $Path -Value $NewVersion -NoNewline

# 4. Commit the new version file and Tag
git add $Path
git commit -m "Bump version to $NewVersion"
git tag $NewVersion
git push --tags

Write-Host "New Compliant Version: $NewVersion" -ForegroundColor Green