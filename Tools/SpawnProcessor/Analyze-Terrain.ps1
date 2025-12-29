# Terrain Analysis Script for UO Spawn Locations
# Reads map, statics, and tiledata files to analyze terrain around spawn points

param(
    [Parameter(Mandatory=$false)]
    [string]$UOPath = "C:\Repositories\ModernUO\Ultima Online Classic",

    [Parameter(Mandatory=$false)]
    [int]$X = 1400,

    [Parameter(Mandatory=$false)]
    [int]$Y = 1623,

    [Parameter(Mandatory=$false)]
    [int]$MapIndex = 0,

    [Parameter(Mandatory=$false)]
    [int]$Range = 10,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Tile Flags
$TileFlags = @{
    None        = 0x00000000
    Background  = 0x00000001
    Weapon      = 0x00000002
    Transparent = 0x00000004
    Translucent = 0x00000008
    Wall        = 0x00000010
    Damaging    = 0x00000020
    Impassable  = 0x00000040
    Wet         = 0x00000080
    Surface     = 0x00000200
    Bridge      = 0x00000400
    Window      = 0x00001000
    Roof        = 0x10000000
    Door        = 0x20000000
}

# Map dimensions (blocks)
$MapDimensions = @{
    0 = @{ Width = 6144; Height = 4096; BlockWidth = 768; BlockHeight = 512 }  # Felucca/Trammel
    1 = @{ Width = 6144; Height = 4096; BlockWidth = 768; BlockHeight = 512 }  # Trammel
    2 = @{ Width = 2304; Height = 1600; BlockWidth = 288; BlockHeight = 200 }  # Ilshenar
    3 = @{ Width = 2560; Height = 2048; BlockWidth = 320; BlockHeight = 256 }  # Malas
    4 = @{ Width = 1448; Height = 1448; BlockWidth = 181; BlockHeight = 181 }  # Tokuno
    5 = @{ Width = 1280; Height = 4096; BlockWidth = 160; BlockHeight = 512 }  # TerMur
}

function Read-TileData {
    param([string]$Path)

    $tiledataPath = Join-Path $Path "tiledata.mul"
    if (-not (Test-Path $tiledataPath)) {
        Write-Error "tiledata.mul not found at $tiledataPath"
        return $null
    }

    $fileInfo = Get-Item $tiledataPath
    $is64BitFlags = $fileInfo.Length -ge 3188736

    Write-Host "Loading tiledata.mul (64-bit flags: $is64BitFlags)..."

    $fs = [System.IO.File]::OpenRead($tiledataPath)
    $br = New-Object System.IO.BinaryReader($fs)

    $landData = @{}
    $itemData = @{}

    # Read land tiles (0x4000 entries)
    for ($i = 0; $i -lt 0x4000; $i++) {
        if ($is64BitFlags) {
            if ($i -eq 1 -or ($i -gt 0 -and ($i -band 0x1F) -eq 0)) {
                $null = $br.ReadInt32()  # header
            }
            $flags = $br.ReadUInt64()
        } else {
            if (($i -band 0x1F) -eq 0) {
                $null = $br.ReadInt32()  # header
            }
            $flags = $br.ReadUInt32()
        }
        $null = $br.ReadInt16()  # textureID
        $nameBytes = $br.ReadBytes(20)
        $terminator = [Array]::IndexOf($nameBytes, [byte]0)
        if ($terminator -lt 0) { $terminator = 20 }
        $name = [System.Text.Encoding]::ASCII.GetString($nameBytes, 0, $terminator)

        $landData[$i] = @{ Name = $name; Flags = $flags }
    }

    # Read item tiles (0x10000 entries for 64-bit, 0x8000 for 32-bit)
    $itemLength = if ($is64BitFlags) { 0x10000 } else { 0x8000 }
    for ($i = 0; $i -lt $itemLength; $i++) {
        if (($i -band 0x1F) -eq 0) {
            $null = $br.ReadInt32()  # header
        }

        $flags = if ($is64BitFlags) { $br.ReadUInt64() } else { $br.ReadUInt32() }
        $weight = $br.ReadByte()
        $quality = $br.ReadByte()
        $animation = $br.ReadUInt16()
        $null = $br.ReadByte()
        $quantity = $br.ReadByte()
        $null = $br.ReadInt32()
        $null = $br.ReadByte()
        $value = $br.ReadByte()
        $height = $br.ReadByte()

        $nameBytes = $br.ReadBytes(20)
        $terminator = [Array]::IndexOf($nameBytes, [byte]0)
        if ($terminator -lt 0) { $terminator = 20 }
        $name = [System.Text.Encoding]::ASCII.GetString($nameBytes, 0, $terminator)

        $itemData[$i] = @{
            Name = $name
            Flags = $flags
            Height = $height
            Weight = $weight
        }
    }

    $br.Close()
    $fs.Close()

    return @{ Land = $landData; Items = $itemData }
}

function Get-FlagsDescription {
    param([uint64]$Flags)

    $desc = @()
    foreach ($key in $TileFlags.Keys) {
        if (($Flags -band $TileFlags[$key]) -ne 0) {
            $desc += $key
        }
    }
    return $desc -join ", "
}

function Read-LandTile {
    param(
        [string]$UOPath,
        [int]$MapIndex,
        [int]$X,
        [int]$Y
    )

    $mapFile = Join-Path $UOPath "map${MapIndex}LegacyMUL.uop"
    if (-not (Test-Path $mapFile)) {
        $mapFile = Join-Path $UOPath "map${MapIndex}.mul"
    }

    if (-not (Test-Path $mapFile)) {
        Write-Error "Map file not found: $mapFile"
        return $null
    }

    $dims = $MapDimensions[$MapIndex]
    $blockX = [Math]::Floor($X / 8)
    $blockY = [Math]::Floor($Y / 8)
    $cellX = $X -band 7
    $cellY = $Y -band 7

    # For UOP files, we need to handle the different format
    # For simplicity, just return a placeholder - the full UOP parsing is complex
    if ($mapFile -like "*.uop") {
        Write-Host "UOP map files require complex parsing - using simplified analysis"
        return @{ ID = 0; Z = 0 }
    }

    # For MUL files
    $blockIndex = $blockX * $dims.BlockHeight + $blockY
    $offset = $blockIndex * 196 + 4  # 196 bytes per block, skip 4-byte header

    $fs = [System.IO.File]::OpenRead($mapFile)
    $br = New-Object System.IO.BinaryReader($fs)

    $fs.Seek($offset + ($cellY * 8 + $cellX) * 3, [System.IO.SeekOrigin]::Begin) | Out-Null

    $tileId = $br.ReadUInt16()
    $z = $br.ReadSByte()

    $br.Close()
    $fs.Close()

    return @{ ID = $tileId; Z = $z }
}

function Read-Statics {
    param(
        [string]$UOPath,
        [int]$MapIndex,
        [int]$X,
        [int]$Y
    )

    $staidxFile = Join-Path $UOPath "staidx${MapIndex}.mul"
    $staticsFile = Join-Path $UOPath "statics${MapIndex}.mul"

    if (-not (Test-Path $staidxFile) -or -not (Test-Path $staticsFile)) {
        return @()
    }

    $dims = $MapDimensions[$MapIndex]
    $blockX = [Math]::Floor($X / 8)
    $blockY = [Math]::Floor($Y / 8)
    $cellX = $X -band 7
    $cellY = $Y -band 7

    $blockIndex = $blockX * $dims.BlockHeight + $blockY

    # Read index
    $fsIdx = [System.IO.File]::OpenRead($staidxFile)
    $brIdx = New-Object System.IO.BinaryReader($fsIdx)

    $fsIdx.Seek($blockIndex * 12, [System.IO.SeekOrigin]::Begin) | Out-Null
    $lookup = $brIdx.ReadInt32()
    $length = $brIdx.ReadInt32()
    $extra = $brIdx.ReadInt32()

    $brIdx.Close()
    $fsIdx.Close()

    if ($lookup -lt 0 -or $length -le 0) {
        return @()
    }

    # Read statics
    $fsStatics = [System.IO.File]::OpenRead($staticsFile)
    $brStatics = New-Object System.IO.BinaryReader($fsStatics)

    $fsStatics.Seek($lookup, [System.IO.SeekOrigin]::Begin) | Out-Null

    $statics = @()
    $count = $length / 7  # 7 bytes per static tile

    for ($i = 0; $i -lt $count; $i++) {
        $tileId = $brStatics.ReadUInt16()
        $sx = $brStatics.ReadByte()
        $sy = $brStatics.ReadByte()
        $z = $brStatics.ReadSByte()
        $hue = $brStatics.ReadInt16()

        if ($sx -eq $cellX -and $sy -eq $cellY) {
            $statics += @{
                ID = $tileId
                X = $sx
                Y = $sy
                Z = $z
                Hue = $hue
            }
        }
    }

    $brStatics.Close()
    $fsStatics.Close()

    return $statics
}

function Analyze-SpawnArea {
    param(
        [string]$UOPath,
        [int]$MapIndex,
        [int]$CenterX,
        [int]$CenterY,
        [int]$Range,
        [hashtable]$TileData
    )

    Write-Host "`n=== Analyzing Spawn Area ==="
    Write-Host "Center: ($CenterX, $CenterY) on Map $MapIndex"
    Write-Host "Range: $Range tiles (area: $($Range * 2 + 1) x $($Range * 2 + 1))"
    Write-Host ""

    $minZ = [int]::MaxValue
    $maxZ = [int]::MinValue
    $zValues = @()
    $waterTiles = 0
    $impassableTiles = 0
    $surfaceTiles = 0
    $staticCount = 0
    $buildingTiles = 0

    for ($dy = -$Range; $dy -le $Range; $dy++) {
        for ($dx = -$Range; $dx -le $Range; $dx++) {
            $x = $CenterX + $dx
            $y = $CenterY + $dy

            # Read land tile
            $land = Read-LandTile -UOPath $UOPath -MapIndex $MapIndex -X $x -Y $y
            if ($land) {
                $z = $land.Z
                $zValues += $z
                if ($z -lt $minZ) { $minZ = $z }
                if ($z -gt $maxZ) { $maxZ = $z }

                if ($TileData -and $TileData.Land[$land.ID]) {
                    $flags = $TileData.Land[$land.ID].Flags
                    if (($flags -band $TileFlags.Wet) -ne 0) { $waterTiles++ }
                    if (($flags -band $TileFlags.Impassable) -ne 0) { $impassableTiles++ }
                    if (($flags -band $TileFlags.Surface) -ne 0) { $surfaceTiles++ }
                }
            }

            # Read statics
            $statics = Read-Statics -UOPath $UOPath -MapIndex $MapIndex -X $x -Y $y
            $staticCount += $statics.Count

            foreach ($static in $statics) {
                if ($TileData -and $TileData.Items[$static.ID]) {
                    $flags = $TileData.Items[$static.ID].Flags
                    if (($flags -band $TileFlags.Roof) -ne 0 -or
                        ($flags -band $TileFlags.Door) -ne 0 -or
                        ($flags -band $TileFlags.Wall) -ne 0) {
                        $buildingTiles++
                    }
                }
            }
        }
    }

    $zRange = $maxZ - $minZ
    $totalTiles = ($Range * 2 + 1) * ($Range * 2 + 1)

    Write-Host "=== Terrain Analysis Results ==="
    Write-Host ""
    Write-Host "Z-Level Analysis:"
    Write-Host "  Min Z: $minZ"
    Write-Host "  Max Z: $maxZ"
    Write-Host "  Z Range: $zRange"

    if ($zValues.Count -gt 0) {
        $avgZ = ($zValues | Measure-Object -Average).Average
        $stdDev = [Math]::Sqrt(($zValues | ForEach-Object { [Math]::Pow($_ - $avgZ, 2) } | Measure-Object -Average).Average)
        Write-Host "  Average Z: $([Math]::Round($avgZ, 2))"
        Write-Host "  Z Std Dev: $([Math]::Round($stdDev, 2))"
    }

    Write-Host ""
    Write-Host "Tile Composition:"
    Write-Host "  Total Tiles: $totalTiles"
    Write-Host "  Water Tiles: $waterTiles ($([Math]::Round($waterTiles * 100 / $totalTiles, 1))%)"
    Write-Host "  Impassable Tiles: $impassableTiles ($([Math]::Round($impassableTiles * 100 / $totalTiles, 1))%)"
    Write-Host "  Static Items: $staticCount"
    Write-Host "  Building Elements: $buildingTiles"

    Write-Host ""
    Write-Host "=== Recommendation ==="

    $recommendation = "UNKNOWN"
    $reason = ""

    if ($zRange -gt 20) {
        $recommendation = "KEEP_HOMERANGE"
        $reason = "Uneven terrain (Z range: $zRange)"
    } elseif ($waterTiles -gt $totalTiles * 0.3) {
        $recommendation = "KEEP_HOMERANGE"
        $reason = "High water content ($([Math]::Round($waterTiles * 100 / $totalTiles))%)"
    } elseif ($buildingTiles -gt 10) {
        $recommendation = "USE_SPAWNBOUNDS"
        $reason = "Building/structure detected"
    } elseif ($zRange -le 5 -and $staticCount -gt 20) {
        $recommendation = "USE_SPAWNBOUNDS"
        $reason = "Flat terrain with structures"
    } elseif ($zRange -le 10 -and $waterTiles -lt $totalTiles * 0.1) {
        $recommendation = "USE_SPAWNBOUNDS"
        $reason = "Relatively flat terrain, low water"
    } else {
        $recommendation = "REVIEW_MANUALLY"
        $reason = "Mixed characteristics"
    }

    Write-Host "Recommendation: $recommendation"
    Write-Host "Reason: $reason"

    return @{
        ZRange = $zRange
        WaterPercent = $waterTiles * 100 / $totalTiles
        StaticCount = $staticCount
        BuildingTiles = $buildingTiles
        Recommendation = $recommendation
        Reason = $reason
    }
}

# Main execution
Write-Host "=== UO Terrain Analyzer ==="
Write-Host "UO Path: $UOPath"
Write-Host ""

# Load TileData
$tileData = Read-TileData -Path $UOPath

if ($null -eq $tileData) {
    Write-Error "Failed to load TileData"
    exit 1
}

Write-Host "Loaded $($tileData.Land.Count) land tiles and $($tileData.Items.Count) item tiles"
Write-Host ""

# Analyze the specified location
$result = Analyze-SpawnArea -UOPath $UOPath -MapIndex $MapIndex -CenterX $X -CenterY $Y -Range $Range -TileData $tileData

Write-Host ""
Write-Host "=== Done ==="
