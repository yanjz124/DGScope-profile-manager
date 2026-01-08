$ErrorActionPreference = "Stop"

Write-Host "=== Testing ZNY TRACON Loading ===" -ForegroundColor Cyan

$znyPath = "$env:LOCALAPPDATA\CRC\ARTCCs\ZNY.json"
$json = Get-Content $znyPath | ConvertFrom-Json

Write-Host "`nZNY.json structure:"
Write-Host "  Has facility: $($null -ne $json.facility)"
Write-Host "  Has childFacilities: $($null -ne $json.facility.childFacilities)"
Write-Host "  ChildFacilities count: $($json.facility.childFacilities.Count)"

Write-Host "`n=== Facility Analysis ===" -ForegroundColor Yellow
$keywords = @("TRACON", "RAPCON", "CERAP", "RATCF")

foreach ($f in $json.facility.childFacilities) {
    $hasId = ![string]::IsNullOrEmpty($f.id)
    $hasName = ![string]::IsNullOrEmpty($f.name)
    $hasStars = $null -ne $f.starsConfiguration
    
    $isControlled = $false
    foreach ($kw in $keywords) {
        if ($f.type -match $kw) {
            $isControlled = $true
            break
        }
    }
    
    $shouldInclude = $hasId -and $hasName -and ($isControlled -or $hasStars)
    
    $color = if ($shouldInclude) { "Green" } else { "Red" }
    $symbol = if ($shouldInclude) { "[+]" } else { "[-]" }
    
    Write-Host "$symbol $($f.id) ($($f.type))" -ForegroundColor $color
    Write-Host "    HasId=$hasId, HasName=$hasName, IsControlled=$isControlled, HasStars=$hasStars"
    Write-Host "    -> Should include: $shouldInclude"
}

Write-Host "`n=== Expected Result ===" -ForegroundColor Cyan
$expected = ($json.facility.childFacilities | Where-Object {
    $hasId = ![string]::IsNullOrEmpty($_.id)
    $hasName = ![string]::IsNullOrEmpty($_.name)
    $hasStars = $null -ne $_.starsConfiguration
    $isControlled = $false
    foreach ($kw in $keywords) {
        if ($_.type -match $kw) {
            $isControlled = $true
            break
        }
    }
    $hasId -and $hasName -and ($isControlled -or $hasStars)
}).Count

Write-Host "Expected TRACON count: $expected"
