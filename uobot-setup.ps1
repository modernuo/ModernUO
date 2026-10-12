<#
Builds a ModernUO shard with the bots from this repository, on Windows.

    powershell -ExecutionPolicy Bypass -File .\setup.ps1 [-Target <folder>]

Target: where ModernUO goes (default: a folder called ModernUO next to this repository). The script clones ModernUO
at the commit the bots are built against, copies the bots and their configuration in, applies the engine patches,
registers the two bot assemblies and builds everything. Run it again after pulling this repository: it copies the
new files over, skips what is already done and rebuilds.
#>
param([string]$Target)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $Target) {
    $Target = Join-Path (Split-Path -Parent $here) 'ModernUO'
}

$pin = 'be3a085'
$url = 'https://github.com/modernuo/ModernUO.git'

if ($env:MODERNUO_URL) {
    $url = $env:MODERNUO_URL
}

function Fail([string]$why) {
    Write-Host "setup: $why" -ForegroundColor Red
    exit 1
}

foreach ($tool in 'git', 'dotnet') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Fail "$tool is needed (git: https://git-scm.com, dotnet: the .NET 10 SDK)"
    }
}

# 1. ModernUO, at the commit the bots are built against
if (-not (Test-Path (Join-Path $Target '.git'))) {
    Write-Host "setup: cloning ModernUO into $Target"
    git clone --quiet $url $Target
    if ($LASTEXITCODE -ne 0) { Fail 'git clone failed' }
    git -C $Target checkout --quiet $pin
    if ($LASTEXITCODE -ne 0) { Fail "could not check out $pin" }
}

# 2. The bots and the configuration they run with (robocopy: exit codes below 8 are success)
robocopy (Join-Path $here 'Projects') (Join-Path $Target 'Projects') /E /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { Fail 'copying Projects failed' }
robocopy (Join-Path $here 'Distribution') (Join-Path $Target 'Distribution') /E /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { Fail 'copying Distribution failed' }

# 3. The engine patches, each once
foreach ($patch in Get-ChildItem (Join-Path $Target 'Projects\BotAIv2\engine-patches\*.patch')) {
    git -C $Target apply --reverse --check $patch.FullName 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "setup: $($patch.Name) is already applied"
        continue
    }

    git -C $Target apply $patch.FullName
    if ($LASTEXITCODE -ne 0) { Fail "$($patch.Name) did not apply" }
    Write-Host "setup: applied $($patch.Name)"
}

# 4. The two bot assemblies, registered once: in the solution, and in the list the server loads
$sln = Join-Path $Target 'ModernUO.slnx'
$text = [IO.File]::ReadAllText($sln)
$nl = "`n"
if ($text.Contains("`r`n")) { $nl = "`r`n" }

if (-not $text.Contains('Projects/BotAIv2/BotAIv2.csproj')) {
    $app = '  <Project Path="Projects/Application/Application.csproj" />'
    $add = $app + $nl + '  <Project Path="Projects/BotAIv2/BotAIv2.csproj" />' + $nl + '  <Project Path="Projects/BotAIv2/mindedBots/BotMindAI.csproj" />'
    [IO.File]::WriteAllText($sln, $text.Replace($app, $add))
    Write-Host 'setup: registered the bots in ModernUO.slnx'
}

$asm = Join-Path $Target 'Distribution\Data\assemblies.json'
$json = [IO.File]::ReadAllText($asm)

if (-not $json.Contains('BotAIv2.dll')) {
    $json = $json.Replace('"UOContent.dll"', '"UOContent.dll",' + $nl + '  "BotAIv2.dll",' + $nl + '  "BotMindAI.dll"')
    [IO.File]::WriteAllText($asm, $json)
    Write-Host 'setup: registered the bots in Distribution\Data\assemblies.json'
}

# 5. Build
Write-Host 'setup: building (the first build takes a few minutes)'
dotnet build $sln -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Fail 'the build failed' }

Write-Host ''
Write-Host 'setup: done. Start the shard once and answer its questions (the client files, the expansion: UOR, the owner):'
Write-Host ''
Write-Host "    cd `"$Target\Distribution`""
Write-Host '    dotnet ModernUO.dll'
