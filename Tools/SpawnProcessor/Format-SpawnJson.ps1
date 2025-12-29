# Format-SpawnJson.ps1
# Formats spawn JSON files with compact arrays and entries
# - location arrays on one line: [x, y, z]
# - spawnBounds arrays on one line: [x1, y1, z1, x2, y2, z2]
# - entry objects each on one line: { "name": "...", "maxCount": N, "probability": N }

param(
    [Parameter(Mandatory=$false)]
    [string]$Path = "C:\Repositories\ModernUO\Distribution\Data\Spawns",

    [Parameter(Mandatory=$false)]
    [switch]$Recurse = $true,

    [Parameter(Mandatory=$false)]
    [switch]$WhatIf = $false
)

function Format-SpawnFile {
    param([string]$FilePath)

    try {
        $content = Get-Content -Path $FilePath -Raw
        $json = $content | ConvertFrom-Json

        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine("[")

        for ($i = 0; $i -lt $json.Count; $i++) {
            $spawn = $json[$i]
            [void]$sb.AppendLine("  {")

            # Get all properties in preferred order
            $orderedKeys = @("guid", "type", "location", "map", "homeRange", "walkingRange",
                            "minDelay", "maxDelay", "team", "count", "spawnBounds", "entries")
            $allKeys = $spawn.PSObject.Properties.Name
            $sortedKeys = @($orderedKeys | Where-Object { $_ -in $allKeys }) +
                         @($allKeys | Where-Object { $_ -notin $orderedKeys })

            for ($j = 0; $j -lt $sortedKeys.Count; $j++) {
                $key = $sortedKeys[$j]
                $value = $spawn.$key
                $isLast = ($j -eq $sortedKeys.Count - 1)
                $comma = if ($isLast) { "" } else { "," }

                switch ($key) {
                    "location" {
                        if ($value -is [array] -and $value.Count -ge 3) {
                            [void]$sb.AppendLine("    `"$key`": [$($value[0]), $($value[1]), $($value[2])]$comma")
                        } else {
                            [void]$sb.AppendLine("    `"$key`": $($value | ConvertTo-Json -Compress)$comma")
                        }
                    }
                    "spawnBounds" {
                        if ($value -is [array] -and $value.Count -ge 6) {
                            [void]$sb.AppendLine("    `"$key`": [$($value[0]), $($value[1]), $($value[2]), $($value[3]), $($value[4]), $($value[5])]$comma")
                        } else {
                            [void]$sb.AppendLine("    `"$key`": $($value | ConvertTo-Json -Compress)$comma")
                        }
                    }
                    "entries" {
                        if ($value -is [array]) {
                            [void]$sb.AppendLine("    `"$key`": [")
                            for ($k = 0; $k -lt $value.Count; $k++) {
                                $entry = $value[$k]
                                $name = $entry.name
                                $maxCount = $entry.maxCount
                                $probability = $entry.probability
                                $entryComma = if ($k -eq $value.Count - 1) { "" } else { "," }
                                [void]$sb.AppendLine("      { `"name`": `"$name`", `"maxCount`": $maxCount, `"probability`": $probability }$entryComma")
                            }
                            [void]$sb.AppendLine("    ]$comma")
                        } else {
                            [void]$sb.AppendLine("    `"$key`": $($value | ConvertTo-Json -Compress)$comma")
                        }
                    }
                    default {
                        $jsonValue = $value | ConvertTo-Json -Compress
                        [void]$sb.AppendLine("    `"$key`": $jsonValue$comma")
                    }
                }
            }

            $spawnComma = if ($i -eq $json.Count - 1) { "" } else { "," }
            [void]$sb.AppendLine("  }$spawnComma")
        }

        [void]$sb.AppendLine("]")

        $formatted = $sb.ToString()

        if ($WhatIf) {
            Write-Host "Would format: $FilePath"
        } else {
            Set-Content -Path $FilePath -Value $formatted -NoNewline -Encoding UTF8
            Write-Host "Formatted: $FilePath"
        }

        return $true
    }
    catch {
        Write-Warning "Error formatting $FilePath`: $_"
        return $false
    }
}

# Main execution
Write-Host "=== Spawn JSON Formatter ==="
Write-Host "Path: $Path"
Write-Host "Recurse: $Recurse"
if ($WhatIf) { Write-Host "Mode: DRY RUN (no changes)" }
Write-Host ""

$searchOption = if ($Recurse) { [System.IO.SearchOption]::AllDirectories } else { [System.IO.SearchOption]::TopDirectoryOnly }

if (Test-Path -Path $Path -PathType Container) {
    $files = Get-ChildItem -Path $Path -Filter "*.json" -Recurse:$Recurse
} elseif (Test-Path -Path $Path -PathType Leaf) {
    $files = @(Get-Item -Path $Path)
} else {
    Write-Error "Path not found: $Path"
    exit 1
}

$successCount = 0
$failCount = 0

foreach ($file in $files) {
    if (Format-SpawnFile -FilePath $file.FullName) {
        $successCount++
    } else {
        $failCount++
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Files processed: $($successCount + $failCount)"
Write-Host "Success: $successCount"
Write-Host "Failed: $failCount"
