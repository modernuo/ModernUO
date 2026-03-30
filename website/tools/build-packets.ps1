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
$docsDir = (Resolve-Path (Join-Path $scriptDir "..")).Path
$packetsDir = Join-Path $docsDir "packets"
$templateFile = Join-Path $packetsDir "template.html"

if (-not $OutputPath) {
    $OutputPath = Join-Path $packetsDir "packets.html"
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
    $incomingFiles = Get-ChildItem "$incomingDir\*.json" -ErrorAction SilentlyContinue | Sort-Object Name
    foreach ($file in $incomingFiles) {
        Write-Host "  Reading: $($file.Name)" -ForegroundColor Gray
        try {
            $content = Get-Content $file.FullName -Raw -Encoding UTF8
            $data = $content | ConvertFrom-Json
            if ($data.packets) {
                # Add category and merge tags for each packet
                foreach ($packet in $data.packets) {
                    # Add category if not present (backward compatibility)
                    if (-not $packet.category -and $data.category) {
                        $packet | Add-Member -NotePropertyName "category" -NotePropertyValue $data.category -Force
                    }

                    # Merge tags: file-level + packet-level
                    $fileTags = @()
                    if ($data.tags) { $fileTags = @($data.tags) }

                    $packetTags = @()
                    if ($packet.tags) { $packetTags = @($packet.tags) }

                    $allTags = @($fileTags + $packetTags) | Select-Object -Unique

                    # Always include the primary category as a tag (first position)
                    if ($data.category -and $data.category -notin $allTags) {
                        $allTags = @($data.category) + $allTags
                    }

                    # Ensure tags is always an array (PowerShell can collapse single-element arrays)
                    $allTags = @($allTags)

                    $packet | Add-Member -NotePropertyName "tags" -NotePropertyValue $allTags -Force
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
    $outgoingFiles = Get-ChildItem "$outgoingDir\*.json" -ErrorAction SilentlyContinue | Sort-Object Name
    foreach ($file in $outgoingFiles) {
        Write-Host "  Reading: $($file.Name)" -ForegroundColor Gray
        try {
            $content = Get-Content $file.FullName -Raw -Encoding UTF8
            $data = $content | ConvertFrom-Json
            if ($data.packets) {
                # Add category and merge tags for each packet
                foreach ($packet in $data.packets) {
                    # Add category if not present (backward compatibility)
                    if (-not $packet.category -and $data.category) {
                        $packet | Add-Member -NotePropertyName "category" -NotePropertyValue $data.category -Force
                    }

                    # Merge tags: file-level + packet-level
                    $fileTags = @()
                    if ($data.tags) { $fileTags = @($data.tags) }

                    $packetTags = @()
                    if ($packet.tags) { $packetTags = @($packet.tags) }

                    $allTags = @($fileTags + $packetTags) | Select-Object -Unique

                    # Always include the primary category as a tag (first position)
                    if ($data.category -and $data.category -notin $allTags) {
                        $allTags = @($data.category) + $allTags
                    }

                    # Ensure tags is always an array (PowerShell can collapse single-element arrays)
                    $allTags = @($allTags)

                    $packet | Add-Member -NotePropertyName "tags" -NotePropertyValue $allTags -Force
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

# Sort packets by ID for deterministic output
# Handle IDs like "0x02" and "0xBF/0x05" (split on / and parse first part)
$allPackets.incoming = @($allPackets.incoming | Sort-Object {
    $baseId = $_.id -split '/' | Select-Object -First 1
    [Convert]::ToInt32($baseId, 16)
}, {
    if ($_.subId) { [Convert]::ToInt32($_.subId, 16) }
    elseif ($_.id -match '/') { [Convert]::ToInt32(($_.id -split '/' | Select-Object -Last 1), 16) }
    else { 0 }
})
$allPackets.outgoing = @($allPackets.outgoing | Sort-Object {
    $baseId = $_.id -split '/' | Select-Object -First 1
    [Convert]::ToInt32($baseId, 16)
}, {
    if ($_.subId) { [Convert]::ToInt32($_.subId, 16) }
    elseif ($_.id -match '/') { [Convert]::ToInt32(($_.id -split '/' | Select-Object -Last 1), 16) }
    else { 0 }
})

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
