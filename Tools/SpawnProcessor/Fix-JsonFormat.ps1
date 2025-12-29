# Fix JSON formatting for spawn files
# Compacts location arrays and entry objects to single lines

param(
    [string]$Path = "C:\Repositories\ModernUO\Distribution\Data\Spawns"
)

$files = Get-ChildItem -Path $Path -Filter "*.json" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    $modified = $false

    # Compact location arrays: "location": [\n      x,\n      y,\n      z\n    ] -> "location": [x, y, z]
    $locationPattern = '"location":\s*\[\s*(\d+),\s*(\d+),\s*(-?\d+)\s*\]'
    if ($content -match '\n\s*\d+,\s*\n') {
        $content = [regex]::Replace($content, '"location":\s*\[\s*(\d+),\s*(\d+),\s*(-?\d+)\s*\]', '"location": [$1, $2, $3]')
        $modified = $true
    }

    # Compact spawnBounds arrays: "spawnBounds": [\n      x1,\n      y1,\n      z1,\n      x2,\n      y2,\n      z2\n    ]
    $spawnBoundsPattern = '"spawnBounds":\s*\[\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)\s*\]'
    if ($content -match '"spawnBounds":\s*\[') {
        $content = [regex]::Replace($content, $spawnBoundsPattern, '"spawnBounds": [$1, $2, $3, $4, $5, $6]')
        $modified = $true
    }

    # Compact entry objects onto single lines
    # Match entry objects and compact them
    $entryPattern = '\{\s*"name":\s*"([^"]+)",\s*"maxCount":\s*(\d+),\s*"probability":\s*(\d+)\s*\}'
    if ($content -match '"entries":\s*\[') {
        $content = [regex]::Replace($content, $entryPattern, '{ "name": "$1", "maxCount": $2, "probability": $3 }')
        $modified = $true
    }

    # Compact entries array to have each entry on its own line
    # First, handle the entries array formatting
    $entriesBlockPattern = '("entries":\s*\[)\s*(\{[^\]]+)\s*(\])'

    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "Fixed: $($file.FullName)"
    }
}

Write-Host "Done!"
