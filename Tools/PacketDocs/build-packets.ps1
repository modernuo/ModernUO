# build-packets.ps1
# Combines all packet JSON files with the HTML template to generate the final documentation page.
#
# Usage:
#   .\build-packets.ps1
#   .\build-packets.ps1 -OutputPath "C:\custom\output\packets.html"

param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

# Determine paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
$dataDir = Join-Path $rootDir "Distribution\Data"
$packetsDir = Join-Path $dataDir "packets"
$templateFile = Join-Path $dataDir "packets.html"

if (-not $OutputPath) {
    $OutputPath = Join-Path $dataDir "web\packets.html"
}

Write-Host "ModernUO Packet Documentation Builder" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verify template exists
if (-not (Test-Path $templateFile)) {
    Write-Error "Template file not found: $templateFile"
    exit 1
}

# Collect all packet JSON files
$allPackets = @{
    incoming = @()
    outgoing = @()
}

$incomingDir = Join-Path $packetsDir "incoming"
$outgoingDir = Join-Path $packetsDir "outgoing"

# Read incoming packets
if (Test-Path $incomingDir) {
    $incomingFiles = Get-ChildItem "$incomingDir\*.json" -ErrorAction SilentlyContinue
    foreach ($file in $incomingFiles) {
        Write-Host "  Reading: $($file.Name)" -ForegroundColor Gray
        try {
            $content = Get-Content $file.FullName -Raw -Encoding UTF8
            $data = $content | ConvertFrom-Json
            if ($data.packets) {
                # Add category to each packet if not present
                foreach ($packet in $data.packets) {
                    if (-not $packet.category -and $data.category) {
                        $packet | Add-Member -NotePropertyName "category" -NotePropertyValue $data.category -Force
                    }
                }
                $allPackets.incoming += $data.packets
            }
        } catch {
            Write-Warning "Failed to parse $($file.Name): $_"
        }
    }
    Write-Host "  Found $($allPackets.incoming.Count) incoming packets" -ForegroundColor Green
} else {
    Write-Host "  No incoming packets directory found" -ForegroundColor Yellow
}

# Read outgoing packets
if (Test-Path $outgoingDir) {
    $outgoingFiles = Get-ChildItem "$outgoingDir\*.json" -ErrorAction SilentlyContinue
    foreach ($file in $outgoingFiles) {
        Write-Host "  Reading: $($file.Name)" -ForegroundColor Gray
        try {
            $content = Get-Content $file.FullName -Raw -Encoding UTF8
            $data = $content | ConvertFrom-Json
            if ($data.packets) {
                # Add category to each packet if not present
                foreach ($packet in $data.packets) {
                    if (-not $packet.category -and $data.category) {
                        $packet | Add-Member -NotePropertyName "category" -NotePropertyValue $data.category -Force
                    }
                }
                $allPackets.outgoing += $data.packets
            }
        } catch {
            Write-Warning "Failed to parse $($file.Name): $_"
        }
    }
    Write-Host "  Found $($allPackets.outgoing.Count) outgoing packets" -ForegroundColor Green
} else {
    Write-Host "  No outgoing packets directory found" -ForegroundColor Yellow
}

$totalPackets = $allPackets.incoming.Count + $allPackets.outgoing.Count
Write-Host ""
Write-Host "Total packets: $totalPackets" -ForegroundColor Cyan

if ($totalPackets -eq 0) {
    Write-Warning "No packets found! The output will have empty documentation."
}

# Convert to JSON
$packetsJson = $allPackets | ConvertTo-Json -Depth 20 -Compress

# HTML entity escape the JSON to safely embed in HTML
$packetsJson = $packetsJson -replace '<', '&lt;' -replace '>', '&gt;'

# Read template and inject JSON
Write-Host ""
Write-Host "Reading template..." -ForegroundColor Gray
$template = Get-Content $templateFile -Raw -Encoding UTF8

Write-Host "Injecting packet data..." -ForegroundColor Gray
$output = $template -replace '<!-- PACKETS -->', $packetsJson

# Ensure output directory exists
$outputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $outputDir)) {
    Write-Host "Creating output directory: $outputDir" -ForegroundColor Gray
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# Write output
Write-Host "Writing output..." -ForegroundColor Gray
$output | Set-Content $OutputPath -Encoding UTF8

$outputSize = (Get-Item $OutputPath).Length
$outputSizeKB = [math]::Round($outputSize / 1024, 2)

Write-Host ""
Write-Host "Success!" -ForegroundColor Green
Write-Host "  Output: $OutputPath" -ForegroundColor White
Write-Host "  Size: $outputSizeKB KB" -ForegroundColor White
Write-Host "  Packets: $totalPackets" -ForegroundColor White
