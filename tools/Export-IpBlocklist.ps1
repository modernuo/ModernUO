#requires -Version 7.0
<#
.SYNOPSIS
    Downloads a small, non-overlapping set of public IP threat feeds and writes them to a single
    ModernUO blocklist file -- merged, de-duplicated and bogon-filtered.

.DESCRIPTION
    This is the producer half of ModernUO's in-app blocklist gate. It fetches a deliberately THIN feed
    set, merges every source into one global set, drops duplicates and reserved/bogon addresses, then
    writes the result to a plain text file that the shard reads via `file` in
    Configuration/blocklist.json. Nothing is installed and no credentials are needed -- the output is just
    a text file, so this can run on any machine that can reach the shard's Distribution folder.

    It writes the file the shard's `BlocklistFilter` demand-pages against. IPs that actually connect are
    promoted to CrowdSec / the OS firewall by the shard; the OS firewall never has to hold millions of
    entries, which is exactly the scale it cannot handle on Windows.

    Inclusion principle: any category of IP used in OTHER attacks that could plausibly be turned against a
    game server should be blocked -- compromised hosts, botnets, scanners, spam / DDoS-as-a-service bots,
    open proxies and Tor relays. That whole surface is already covered by the anchor feed `bitwire-it`,
    which is itself a 91-source aggregator (it folds in spamhaus, ipsum, firehol-level2, blocklist-de,
    dshield, emergingthreats, binarydefense, cins-army, bruteforceblocker, greensnow, vxvault, ThreatFox,
    StopForumSpam/sblam, Tor, open-proxy and C2 lists). So the inclusive posture lives in the base layer,
    and every one of those standalone feeds is dropped as pure redundancy. Only the feeds bitwire does NOT
    already carry are kept on top of it:

        bitwire-it        2h-refreshed 91-source aggregate (compromised hosts, botnets, scanners, spam
                          bots, Tor/open-proxy abuse relays, ThreatFox C2) -- the broad base layer.
        romainmarcoux     ~130k fresh attacker IPs bitwire's snapshot lags on (high-churn feed).
        sentinel-turris   ~800 unique honeypot probers (Turris greylist) not in bitwire.
        firehol-level1    hijacked/reputation NETBLOCKS (spamhaus DROP-style) -- bogon-filtered.

    The only category deliberately held back is commercial VPN exit endpoints, which could block a legit
    player -- and those are barely present here anyway (bitwire is ~5% of VPN-tunnel lists). If you ever want
    to protect VPN/Tor players, pass -ExcludeAnonymizers to subtract Tor/open-proxy/VPN IPs from the output.

    ALLOWLIST
    Aggregators inevitably list shared consumer address space. A CGNAT public IP fronts many subscribers, so
    one abusive customer gets the address listed and every other subscriber behind it is blocked with them.
    Entries in the allowlist file (-AllowlistFile) are SUBTRACTED from the merged set before it is written, so
    the exemption costs nothing on the shard's accept path -- which is deliberately allowlist-free, because a
    whitelist there could only ever turn a deny into an allow at the price of a lookup on every accept, the
    attacker's included. Allowlisting belongs here (at generation) and at the enforcement layer
    (`cscli allowlists`), never at the gate.

    Subtraction is range-correct: an allowlisted address that falls inside a blocked CIDR splits that CIDR
    around the hole instead of being silently ignored, so an exemption always takes effect no matter which
    shape the feed happened to publish.

    Every `ip-allowlist*.txt` beside the output is subtracted, so allowlists are split by owner rather than
    kept in one file: `ip-allowlist.txt` holds the operator's own exemptions and is never rewritten, while
    network carve-outs live in `ip-allowlist-<name>.txt`. Keeping them apart means a carve-out can be
    regenerated, diffed or copied to another shard without disturbing hand-written entries.

    NO CARVE-OUT IS SHIPPED. Which providers to exempt is a policy call that depends on where a shard's
    players actually are, and a carve-out names a real network, so this script builds them on request rather
    than publishing anyone's. A shard whose players are on CGNAT -- satellite, mobile, or an ISP short on
    IPv4 -- will usually want one:

        .\Export-IpBlocklist.ps1 -AddCarveout starlink -Asn 14593

    That costs roughly 0.1% of the list. Abusive hosts inside a carved-out network are still caught on
    BEHAVIOR by the rate limiter and promoted to CrowdSec, which is the gate that actually observes them.

    OUTPUT FORMAT (must stay in sync with UOContent/Network/Blocklist/BlocklistFile.cs):
        Line 1 is a header comment carrying the version markers, e.g.
            # modernuo-blocklist generated=2026-07-25T18:03:11Z count=3914022 ipv4=3901188 cidr=12834
        The shard polls `reloadInterval` and reloads when the file mtime AND `generated=` change,
        so the header is REQUIRED -- without it the shard loads once and never picks up a new file.
        Every following line is one entry: a bare IPv4/IPv6 address or a CIDR (`1.2.3.0/24`). Blank lines
        and lines starting with `#` or `;` are ignored. Order does not matter; the shard sorts and
        coalesces on load. The feeds used here are IPv4-only, but the shard parses IPv6 lines too.

    The file is written to a `.tmp` sibling and swapped into place atomically, so the shard never reads a
    half-written list -- it either sees the previous version or the new one, whole.

    Performance: bitwire alone is ~4M lines. Parsing/validating/bogon-filtering that in interpreted
    PowerShell is the slow part (minutes), so the hot loop is compiled once via Add-Type (C#) -- it runs in
    ~1s. Downloads stream with a live Write-Progress bar; every phase prints its own elapsed time so you can
    see exactly where the wall-clock goes.

    Requires PowerShell 7 (pwsh), which runs on Windows, Linux and macOS -- Windows PowerShell 5.1 is
    not supported and the script refuses to run there. Schedule it with Task Scheduler, cron, or a
    systemd timer.

    Every run rewrites the whole file, so an IP that drops off the feeds stops being blocked on the next
    run -- there is no TTL to tune. Calling it is idempotent: if the list on disk is younger than
    -MinInterval the script exits without downloading anything, so an over-eager trigger costs nothing
    upstream. -Force overrides that.

.PARAMETER DistributionPath
    Path to the shard's Distribution folder. The blocklist is written to the Configuration/ip-blocklist.txt
    beneath it, which is the default `file` in blocklist.json. Not needed when the script is run from its
    place in the repo (tools/), or when -OutFile is given.

.PARAMETER OutFile
    Explicit output path, overriding -DistributionPath. Use this if you relocated the blocklist and
    changed `file` in blocklist.json to match.

.PARAMETER MinInterval
    Refuse to re-run while the existing blocklist is younger than this (default 2h), so a misbehaving
    scheduler, a login script or a stuck retry loop cannot hammer the upstream feeds. The age comes from
    the `generated=` header of the file already on disk (falling back to its mtime), so it survives across
    machines and reboots -- there is no separate state file. Nothing is downloaded when the check trips.
    Accepts `90s`, `45m`, `2h`, `2.5h`, `1d`, or a bare number of hours. Use `0` to disable the check.
    Match this to how often you actually want fresh data: the anchor feed only refreshes every 2h, so
    running more often than that costs bandwidth and gains nothing.

.PARAMETER Force
    Run regardless of how recently the blocklist was generated (bypasses -MinInterval).

.PARAMETER Feeds
    Which feeds to include (by Name). Default: all of them.

.PARAMETER AllowlistFile
    One or more lists of addresses that must NEVER be blocked; every entry is subtracted from the merged set
    before the output is written. Same format as the blocklist: one bare IPv4 or CIDR per line, `#`/`;`
    comments ignored.

    Defaults to every `ip-allowlist*.txt` beside the output, merged into one allow set:
        ip-allowlist.txt            operator exemptions -- created on first run, never rewritten
        ip-allowlist-<name>.txt     a network carve-out -- generated data, safe to regenerate or copy
    Discovered rather than configured, so a carve-out you add is picked up with no further edits. Blank a
    file (keep the file) to disable its contents; delete it to drop it entirely.

    Passing this parameter replaces the defaults entirely; an explicitly-named file that does not exist is a
    warning rather than a silent template write, so a typo cannot look like it worked. Pass `''` to disable
    subtraction altogether.

    Editing any allowlist also bypasses -MinInterval on the next run: an exemption you just added would
    otherwise sit unapplied for up to the cooldown, which reads exactly like the allowlist not working.

.PARAMETER AddCarveout
    Build a carve-out for a network and start subtracting it. Takes a short name for the file and -Asn for
    the network, fetches that ASN's current routing announcements, collapses them, and writes
    `ip-allowlist-<name>.txt` beside the output. Implies -Force.

        .\Export-IpBlocklist.ps1 -AddCarveout starlink -Asn 14593

    No carve-out ships with this script: naming a network to exempt is a policy call for the shard, so they
    are built on request rather than published here. Re-running with the same name rebuilds the file.

.PARAMETER Asn
    Autonomous system number for -AddCarveout, e.g. 14593 for Starlink. Look one up by querying an address
    the network hands out, or on any public BGP lookup.

.PARAMETER RefreshCarveouts
    Re-fetch every carve-out beside the output and rewrite it from current routing announcements. Implies
    -Force. Carve-outs are recognised by the `asn=` marker in their header, so a hand-written allowlist is
    left alone, and one whose fetch fails keeps the data it already had.

    Announcements rather than ownership records on purpose: registry data disagrees with what is actually
    routed, and registry queries silently cap their result sets.

.PARAMETER ExcludeAnonymizers
    Also download Tor-exit / open-proxy / VPN-tunnel lists and SUBTRACT those IPs from the output. Off by
    default -- for a game server, Tor/open-proxy relays are attack infrastructure you want to block. Turn
    this on only if you need to keep VPN/Tor players reachable.

.PARAMETER DryRun
    Download + parse + merge + count only. Writes nothing.

.EXAMPLE
    .\Export-IpBlocklist.ps1 -DryRun

.EXAMPLE
    # Safe to call as often as you like -- it no-ops unless the list is older than 2h.
    .\Export-IpBlocklist.ps1 -DistributionPath 'C:\Shard\Distribution'

.EXAMPLE
    .\Export-IpBlocklist.ps1 -OutFile 'D:\shared\ip-blocklist.txt' -ExcludeAnonymizers

.EXAMPLE
    # Regenerate right now, ignoring the cooldown.
    .\Export-IpBlocklist.ps1 -DistributionPath 'C:\Shard\Distribution' -Force

.EXAMPLE
    # Unblock a player caught by a shared-IP listing: add the address, then regenerate. Editing the
    # allowlist bypasses the cooldown, so no -Force is needed.
    Add-Content 'C:\Shard\Distribution\Configuration\ip-allowlist.txt' '203.0.113.42'
    .\Export-IpBlocklist.ps1 -DistributionPath 'C:\Shard\Distribution'

.EXAMPLE
    # Check what an allowlist would cost before committing to it.
    .\Export-IpBlocklist.ps1 -AllowlistFile 'D:\shared\allow.txt' -DryRun

.EXAMPLE
    # Exempt a CGNAT provider whose players keep getting listed, then keep it current.
    .\Export-IpBlocklist.ps1 -AddCarveout starlink -Asn 14593
    .\Export-IpBlocklist.ps1 -RefreshCarveouts

.EXAMPLE
    # Linux/macOS, e.g. from cron:
    pwsh -File /opt/modernuo/Export-IpBlocklist.ps1 -DistributionPath /opt/modernuo/Distribution

.NOTES
    Feeds are aggressive-but-low-FP for a game server (attacker / botnet / compromised / abuse-relay SOURCE
    IPs). Reserved/bogon space (0/8, 10/8, 127/8, RFC1918, multicast, etc.) is always filtered out -- this
    matters because firehol-level1 ships bogon netblocks that would otherwise block private/reserved ranges.
#>
[CmdletBinding()]
param(
    [string]   $DistributionPath,
    [string]   $OutFile,
    [string]   $MinInterval = '2h',
    [string[]] $AllowlistFile,
    [string]   $AddCarveout,
    [int]      $Asn,
    [switch]   $RefreshCarveouts,
    [string[]] $Feeds,
    [switch]   $ExcludeAnonymizers,
    [switch]   $Force,
    [switch]   $DryRun
)

$ErrorActionPreference = 'Stop'

# Fail before any download rather than after 60MB of feeds.
if ($AddCarveout -and $Asn -le 0) {
    throw "-AddCarveout needs -Asn, e.g. -AddCarveout starlink -Asn 14593"
}
$UA = 'ModernUO-Blocklist-Export'
$totalSw = [System.Diagnostics.Stopwatch]::StartNew()

# Default location under the Distribution folder. Keep in sync with BlocklistSettings.File.
# Kept as separate segments (never a literal 'a\b') so Join-Path picks the right separator per OS.
$DefaultPathSegments = @('Configuration', 'ip-blocklist.txt')

# ---------------------------------------------------------------------------------------------------------
# Resolve the output path. Explicit -OutFile wins; then -DistributionPath; then the in-repo layout
# (tools\ sits next to Distribution\) so a checkout works with no arguments at all. The script is meant to
# be copied onto the shard host, and there it needs -DistributionPath (or -OutFile).
# ---------------------------------------------------------------------------------------------------------
if (-not $OutFile) {
    if (-not $DistributionPath -and $PSScriptRoot) {
        $inRepo = Join-Path (Split-Path -Parent $PSScriptRoot) 'Distribution'
        if (Test-Path -LiteralPath $inRepo -PathType Container) { $DistributionPath = $inRepo }
    }
    if (-not $DistributionPath) {
        throw "Could not locate the shard's Distribution folder. Pass -DistributionPath 'C:\path\to\Distribution' (or -OutFile)."
    }
    if (-not (Test-Path -LiteralPath $DistributionPath -PathType Container)) {
        throw "DistributionPath '$DistributionPath' does not exist."
    }
    $OutFile = Join-Path $DistributionPath @DefaultPathSegments
}

# ---------------------------------------------------------------------------------------------------------
# Network carve-outs are DATA, not code: this script ships none. Which providers a shard exempts is a policy
# call that depends on where its players actually are, so the carve-outs live in files an admin creates with
# -AddCarveout, and every ip-allowlist*.txt beside the output is subtracted.
#
# A shard whose players are on CGNAT -- satellite, mobile, or an ISP short on IPv4 -- will usually want one:
#     .\Export-IpBlocklist.ps1 -AddCarveout starlink -Asn 14593
# ---------------------------------------------------------------------------------------------------------

# ---------------------------------------------------------------------------------------------------------
# Resolve the allowlists. They sit beside the output by default so relocating the blocklist keeps the set
# together, and they are split by owner: `ip-allowlist.txt` is the operator's -- hand-edited, never rewritten
# -- while each carve-out file is generated data that can be regenerated, diffed or copied between shards
# without touching anyone's local exemptions.
#
# Carve-outs are discovered rather than listed, so a file an admin drops in is picked up with no config edit
# and no code change. An EXPLICIT -AllowlistFile replaces the whole set and is never templated: if the
# operator names a file, a missing one is a typo worth hearing about.
# ---------------------------------------------------------------------------------------------------------
$AllowGlob = 'ip-allowlist*.txt'
$ConfigDir = Split-Path -Parent $OutFile

$allowExplicit = $PSBoundParameters.ContainsKey('AllowlistFile')

if ($allowExplicit) {
    $AllowPaths = @($AllowlistFile | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
else {
    $AllowPaths = @(Get-ChildItem -Path $ConfigDir -Filter $AllowGlob -File -ErrorAction SilentlyContinue |
                    Sort-Object Name | ForEach-Object { $_.FullName })
}

function Write-AllowlistFile {
    param([string]$Path, [string[]]$Lines)

    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path -LiteralPath $dir -PathType Container)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    # Same atomic write the blocklist gets: a half-written allowlist would silently under-subtract.
    $tmp = $Path + '.tmp'
    [IO.File]::WriteAllLines($tmp, $Lines, [System.Text.UTF8Encoding]::new($false))
    [IO.File]::Move($tmp, $Path, $true)
}

$OperatorTemplate = @'
# ModernUO blocklist allowlist -- every entry here is SUBTRACTED from the generated blocklist.
#
# This file is yours. Export-IpBlocklist.ps1 creates it once and never rewrites it, so anything you add
# survives every regeneration.
#
# One entry per line: a bare IPv4 address (1.2.3.4) or a CIDR (1.2.3.0/24). Lines starting with '#' or ';'
# are comments. Order does not matter. Re-run Export-IpBlocklist.ps1 to apply changes -- editing this file
# bypasses the -MinInterval cooldown, so no -Force is needed.
#
# Removal is range-correct: an address listed here is removed even when a feed published it as part of a
# larger CIDR -- that CIDR is split around the hole rather than dropped wholesale or silently ignored.
#
# Network carve-outs live in their own ip-allowlist-<name>.txt beside this one (see -AddCarveout), so they
# can be regenerated or copied between shards without touching anything you put here.
#
# Put player/staff exemptions below, one per line, e.g.:
#   203.0.113.42        # shard owner, listed via a shared upstream address
'@

# Carve-out files carry their own `asn=` marker, so -RefreshCarveouts can rebuild whatever an admin created
# without this script keeping a list of anyone's networks.
function Get-CarveoutHeader {
    param([string]$Name, [int]$CarveoutAsn)

    @(
        ("# {0} carve-out (asn={1}) -- subtracted from the generated blocklist." -f $Name, $CarveoutAsn)
        "#"
        "# Reputation feeds list shared consumer address space constantly, so a hit inside a CGNAT network"
        "# says little about the player currently behind it. Abusive hosts here are still caught on BEHAVIOR."
        "#"
        "# GENERATED DATA -- safe to regenerate, diff, or copy to another shard. Blank the file (keep the"
        "# file) to reputation-block this network again; delete it to stop carving it out entirely."
        "#"
        "# Refresh with: .\Export-IpBlocklist.ps1 -RefreshCarveouts"
    )
}

# Fetches a network's currently ANNOUNCED prefixes and collapses them. Routing data, not a registry:
# ownership records disagree with what is actually announced, and registry queries cap their result sets.
function Get-CarveoutPrefixes {
    param([int]$CarveoutAsn)

    $url = "https://stat.ripe.net/data/announced-prefixes/data.json?resource=AS$CarveoutAsn"
    $json = Get-Url -Url $url -Label ("AS{0} prefixes" -f $CarveoutAsn) | ConvertFrom-Json

    $v4 = @($json.data.prefixes.prefix | Where-Object { $_ -and $_ -notmatch ':' })
    if (-not $v4) { throw "AS$CarveoutAsn announced no IPv4 prefixes -- refusing to overwrite the carve-out." }

    [BlocklistExporter]::CollapsePrefixes(($v4 -join "`n"))
}

# Reads the `asn=` marker back out of a carve-out file. Anything without one is a hand-written allowlist and
# is left alone by -RefreshCarveouts.
function Get-CarveoutAsn {
    param([string]$Path)

    foreach ($line in (Get-Content -LiteralPath $Path -TotalCount 5 -ErrorAction SilentlyContinue)) {
        if ($line -match 'asn=(\d+)') { return [int]$Matches[1] }
    }

    return 0
}

# The operator's own list is the one file this script will create unprompted; carve-outs are opt-in.
if (-not $allowExplicit) {
    $operatorPath = Join-Path $ConfigDir 'ip-allowlist.txt'
    if (-not (Test-Path -LiteralPath $operatorPath -PathType Leaf)) {
        Write-AllowlistFile -Path $operatorPath -Lines @($OperatorTemplate)
        Write-Host ("Created allowlist at {0} (add player/staff exemptions here)." -f $operatorPath)
        $AllowPaths = @($operatorPath) + $AllowPaths
    }
}
else {
    $AllowPaths = @($AllowPaths | ForEach-Object {
        if (Test-Path -LiteralPath $_ -PathType Leaf) { return $_ }
        Write-Warning ("Allowlist '{0}' does not exist -- nothing will be subtracted from it. Check the path." -f $_)
    })
}

# ---------------------------------------------------------------------------------------------------------
# Cooldown gate. Runs BEFORE anything is downloaded: the whole point is that a misconfigured scheduler or a
# retry loop cannot spam the upstream feeds. State lives in the output file itself (`generated=` header,
# mtime as fallback), so it is correct across reboots, machines and hand-runs with no sidecar state file.
# ---------------------------------------------------------------------------------------------------------
function ConvertTo-Duration {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return [TimeSpan]::Zero }
    $t = $Text.Trim().ToLowerInvariant()
    $unit = $t[$t.Length - 1]
    $numText = if ($unit -match '[0-9.]') { $t } else { $t.Substring(0, $t.Length - 1) }
    $n = 0.0
    # InvariantCulture is not optional here: under a comma-decimal locale (de-DE, fr-FR, ...) the
    # current-culture parse reads '2.5' as 25 -- it treats '.' as a group separator and SUCCEEDS, so
    # `-MinInterval 2.5h` would silently become a 25 hour cooldown instead of failing loudly.
    if (-not [double]::TryParse($numText, [Globalization.NumberStyles]::Float,
                                [Globalization.CultureInfo]::InvariantCulture, [ref]$n)) {
        throw "Could not parse duration '$Text' (try 90s, 45m, 2h, 2.5h, 1d)."
    }
    switch ($unit) {
        's'     { return [TimeSpan]::FromSeconds($n) }
        'm'     { return [TimeSpan]::FromMinutes($n) }
        'h'     { return [TimeSpan]::FromHours($n) }
        'd'     { return [TimeSpan]::FromDays($n) }
        default { return [TimeSpan]::FromHours($n) }   # bare number == hours
    }
}

# Age of the list already on disk, or $null when there is nothing usable to age.
function Get-BlocklistAge {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }

    # Prefer the header we wrote: it describes the data, not the file, so copying/restoring the file
    # cannot make a stale list look fresh (or a fresh one look stale).
    try {
        $first = Get-Content -LiteralPath $Path -TotalCount 1 -ErrorAction Stop
        if ($first -and $first.StartsWith('#')) {
            foreach ($tok in $first.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)) {
                if ($tok.StartsWith('generated=', [StringComparison]::Ordinal)) {
                    $stamp = [DateTime]::MinValue
                    $styles = [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal
                    if ([DateTime]::TryParse($tok.Substring(10), [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$stamp)) {
                        return @{ Age = ([DateTime]::UtcNow - $stamp); Stamp = $tok.Substring(10); Source = 'header' }
                    }
                }
            }
        }
    }
    catch { }

    # Hand-maintained or truncated file: fall back to the filesystem timestamp.
    try {
        $w = (Get-Item -LiteralPath $Path -ErrorAction Stop).LastWriteTimeUtc
        return @{ Age = ([DateTime]::UtcNow - $w)
                  Stamp = $w.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
                  Source = 'mtime' }
    }
    catch { return $null }
}

$minAge = ConvertTo-Duration $MinInterval
# Asking for carve-out data implies -Force: waiting out the cooldown and leaving the old data in place would
# be the wrong answer.
if (-not $Force -and -not $RefreshCarveouts -and -not $AddCarveout -and $minAge -gt [TimeSpan]::Zero) {
    $existing = Get-BlocklistAge -Path $OutFile
    if ($existing) {
        # A negative age means the stamp is in the future (clock skew, or a file from another host). Treat it
        # as fresh: refusing to run is the recoverable failure, hammering the feeds on every tick is not.
        if ($existing.Age -lt $minAge) {
            # An allowlist edited since the list was built is the one case where waiting out the cooldown is
            # the wrong answer: the operator is unblocking someone, and "nothing happened" is indistinguishable
            # from the allowlist not working. Cheap to honour -- it can only ever shrink the output.
            $changedAllow = $null
            $builtAt = [DateTime]::UtcNow - $existing.Age
            foreach ($p in $AllowPaths) {
                try {
                    if ((Get-Item -LiteralPath $p -ErrorAction Stop).LastWriteTimeUtc -gt $builtAt) {
                        $changedAllow = $p
                        break
                    }
                }
                catch { }
            }

            if ($changedAllow) {
                Write-Host ("Allowlist {0} changed since the blocklist was built; regenerating despite -MinInterval {1}." -f `
                    $changedAllow, $MinInterval)
            }
            else {
                $agoText = if ($existing.Age -lt [TimeSpan]::Zero) { 'in the future -- check the clock' } else { ("{0:N1}h ago" -f $existing.Age.TotalHours) }
                Write-Host ("Blocklist at {0} was generated {1} ({2}={3}); newer than -MinInterval {4}." -f `
                    $OutFile, $agoText, $existing.Source, $existing.Stamp, $MinInterval)
                Write-Host "Nothing downloaded. Pass -Force to regenerate now, or lower -MinInterval."
                return
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------------------
# Compiled hot loop. Interpreted PowerShell chokes on bitwire's ~4M lines; this parses + validates + bogon-
# filters + de-dupes in one compiled pass, and writes the final file directly (no 4M-element PS pipelines).
# Deliberately plain C#: no LINQ, no generics beyond HashSet, nothing that would slow the hot loop.
# ---------------------------------------------------------------------------------------------------------
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class BlocklistExporter
{
    static bool TryParseIPv4(string s, int start, int len, out uint val)
    {
        val = 0;
        uint acc = 0; int octet = 0, dots = 0, digits = 0, end = start + len;
        for (int i = start; i < end; i++)
        {
            char c = s[i];
            if (c == '.')
            {
                if (digits == 0 || octet > 255) return false;
                acc = (acc << 8) | (uint)octet; dots++; octet = 0; digits = 0;
            }
            else if (c >= '0' && c <= '9')
            {
                octet = octet * 10 + (c - '0'); if (++digits > 3) return false;
            }
            else return false;
        }
        if (dots != 3 || digits == 0 || octet > 255) return false;
        val = (acc << 8) | (uint)octet;
        return true;
    }

    static bool IsBogon(uint start, uint end, uint[] bs, uint[] be)
    {
        for (int i = 0; i < bs.Length; i++)
            if (start <= be[i] && end >= bs[i]) return true;
        return false;
    }

    // Parse one feed's text; add bare IPs to `singles`, CIDRs to `cidrs`. Returns count newly added.
    public static int AddContent(string content, HashSet<uint> singles, HashSet<string> cidrs, uint[] bs, uint[] be)
    {
        int added = 0, n = content.Length, i = 0;
        while (i < n)
        {
            int eol = content.IndexOf('\n', i);
            int lineEnd = (eol < 0) ? n : eol;
            int a = i, b = lineEnd;
            while (a < b && (content[a] == ' ' || content[a] == '\t' || content[a] == '\r')) a++;
            while (b > a && (content[b - 1] == ' ' || content[b - 1] == '\t' || content[b - 1] == '\r')) b--;
            i = (eol < 0) ? n : eol + 1;
            if (a >= b) continue;
            char first = content[a];
            if (first == '#' || first == ';') continue;

            // Feeds vary: some are bare IPs, some are CSV/whitespace records with the IP first.
            int t = a;
            while (t < b)
            {
                char c = content[t];
                if (c == ' ' || c == '\t' || c == ',' || c == ';') break;
                t++;
            }
            int slash = -1;
            for (int k = a; k < t; k++) { if (content[k] == '/') { slash = k; break; } }

            if (slash >= 0)
            {
                uint ip;
                if (!TryParseIPv4(content, a, slash - a, out ip)) continue;
                int bits = 0, bd = 0;
                for (int k = slash + 1; k < t; k++)
                {
                    char c = content[k];
                    if (c < '0' || c > '9') { bd = -1; break; }
                    bits = bits * 10 + (c - '0'); bd++;
                }
                if (bd <= 0 || bits > 32) continue;
                ulong size = (bits == 0) ? 0xFFFFFFFFUL : ((1UL << (32 - bits)) - 1UL);
                ulong endAddr = (ulong)ip + size; if (endAddr > 0xFFFFFFFFUL) endAddr = 0xFFFFFFFFUL;
                if (IsBogon(ip, (uint)endAddr, bs, be)) continue;
                if (cidrs.Add(content.Substring(a, t - a))) added++;
            }
            else
            {
                uint ip;
                if (!TryParseIPv4(content, a, t - a, out ip)) continue;
                if (IsBogon(ip, ip, bs, be)) continue;
                if (singles.Add(ip)) added++;
            }
        }
        return added;
    }

    // ---------------------------------------------------------------------------------------------------
    // Allowlist subtraction. Allow entries become sorted, merged [start,end] ranges once; the blocklist is
    // then filtered against them. The interesting case is a blocked CIDR that only PARTIALLY overlaps an
    // allow range -- dropping it whole would unblock far more than asked, keeping it whole would ignore the
    // exemption, so it is split into the surviving pieces and re-emitted as minimal CIDRs.
    // ---------------------------------------------------------------------------------------------------
    public sealed class RangeSet
    {
        public uint[] Start;
        public uint[] End;
        public int Count;
    }

    public sealed class AllowResult
    {
        public int SinglesRemoved;
        public int CidrsDropped;
        public int CidrsSplit;
        public int EntriesAdded;
    }

    // Parses "a.b.c.d/p" to an inclusive range. The base is masked to the prefix, so a sloppy 1.2.3.5/24
    // means the whole 1.2.3.0/24 -- the standard reading, and the safe direction for an exemption.
    static bool TryCidrRange(string c, out ulong lo, out ulong hi)
    {
        lo = 0; hi = 0;
        int slash = c.IndexOf('/');
        if (slash <= 0) return false;
        uint ip;
        if (!TryParseIPv4(c, 0, slash, out ip)) return false;
        int bits = 0, bd = 0;
        for (int i = slash + 1; i < c.Length; i++)
        {
            char ch = c[i];
            if (ch < '0' || ch > '9') { bd = -1; break; }
            bits = bits * 10 + (ch - '0'); bd++;
        }
        if (bd <= 0 || bits > 32) return false;
        // 1u << 32 is undefined in C# (the shift count is masked to 5 bits), so /0 is special-cased.
        uint mask = (bits == 0) ? 0u : ~((uint)((1UL << (32 - bits)) - 1UL));
        ulong size = (bits == 0) ? 0x100000000UL : (1UL << (32 - bits));
        lo = ip & mask;
        hi = lo + size - 1UL;
        return true;
    }

    public static RangeSet BuildRanges(HashSet<uint> singles, HashSet<string> cidrs)
    {
        uint[] s = new uint[singles.Count + cidrs.Count];
        uint[] e = new uint[s.Length];
        int k = 0;

        foreach (uint v in singles) { s[k] = v; e[k] = v; k++; }
        foreach (string c in cidrs)
        {
            ulong lo, hi;
            if (!TryCidrRange(c, out lo, out hi)) continue;
            s[k] = (uint)lo;
            e[k] = (uint)(hi > 0xFFFFFFFFUL ? 0xFFFFFFFFUL : hi);
            k++;
        }

        Array.Resize(ref s, k);
        Array.Resize(ref e, k);
        Array.Sort(s, e);

        // Coalesce overlapping AND adjacent ranges so the lookups below can assume disjoint, ordered spans.
        int w = 0;
        for (int i = 0; i < k; i++)
        {
            if (w > 0 && (ulong)s[i] <= (ulong)e[w - 1] + 1UL)
            {
                if (e[i] > e[w - 1]) e[w - 1] = e[i];
            }
            else
            {
                s[w] = s[i]; e[w] = e[i]; w++;
            }
        }

        return new RangeSet { Start = s, End = e, Count = w };
    }

    // Index of the first range whose End >= v (ranges are disjoint and sorted, so End is sorted too).
    static int FirstEndAtLeast(uint[] re, int n, uint v)
    {
        int lo = 0, hi = n;
        while (lo < hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (re[mid] < v) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    static bool Covered(uint[] rs, uint[] re, int n, uint v)
    {
        int i = FirstEndAtLeast(re, n, v);
        return i < n && rs[i] <= v;
    }

    static string FormatCidr(uint ip, int bits)
    {
        char[] buf = new char[19];
        int p = 0;
        p = WriteOctet(buf, p, (ip >> 24) & 255); buf[p++] = '.';
        p = WriteOctet(buf, p, (ip >> 16) & 255); buf[p++] = '.';
        p = WriteOctet(buf, p, (ip >> 8) & 255);  buf[p++] = '.';
        p = WriteOctet(buf, p, ip & 255);         buf[p++] = '/';
        p = WriteOctet(buf, p, (uint)bits);
        return new string(buf, 0, p);
    }

    // Writes [lo,hi] as the minimal set of aligned CIDR blocks. A /32 goes back to the singles set so the
    // output keeps the file's convention of bare addresses for single hosts.
    static void Emit(ulong lo, ulong hi, HashSet<uint> singles, HashSet<string> cidrs, AllowResult r)
    {
        while (lo <= hi)
        {
            int bits = 32;
            while (bits > 0)
            {
                ulong size = 1UL << (32 - (bits - 1));
                if ((lo % size) != 0UL) break;
                if (lo + size - 1UL > hi) break;
                bits--;
            }

            if (bits == 32)
            {
                if (singles.Add((uint)lo)) r.EntriesAdded++;
            }
            else if (cidrs.Add(FormatCidr((uint)lo, bits)))
            {
                r.EntriesAdded++;
            }

            lo += 1UL << (32 - bits);
        }
    }

    public static AllowResult ApplyAllowlist(HashSet<uint> singles, HashSet<string> cidrs, RangeSet allow)
    {
        var r = new AllowResult();
        if (allow == null || allow.Count == 0) return r;

        uint[] rs = allow.Start, re = allow.End;
        int n = allow.Count;

        // Singles first: the CIDR pass below can add new singles, and those are outside the allow ranges by
        // construction, so re-testing them would be wasted work.
        uint[] sarr = new uint[singles.Count];
        singles.CopyTo(sarr);
        for (int i = 0; i < sarr.Length; i++)
        {
            if (Covered(rs, re, n, sarr[i]) && singles.Remove(sarr[i])) r.SinglesRemoved++;
        }

        string[] carr = new string[cidrs.Count];
        cidrs.CopyTo(carr);
        cidrs.Clear();

        for (int i = 0; i < carr.Length; i++)
        {
            string c = carr[i];
            ulong lo, hi;

            // Unparseable entries are kept verbatim rather than dropped: this pass exists to subtract, and
            // silently discarding something it could not read would weaken the list.
            if (!TryCidrRange(c, out lo, out hi)) { cidrs.Add(c); continue; }
            if (hi > 0xFFFFFFFFUL) hi = 0xFFFFFFFFUL;

            int idx = FirstEndAtLeast(re, n, (uint)lo);
            if (idx >= n || (ulong)rs[idx] > hi) { cidrs.Add(c); continue; } // no overlap: the common case

            ulong cursor = lo;
            int before = r.EntriesAdded;
            for (int j = idx; j < n && (ulong)rs[j] <= hi; j++)
            {
                if ((ulong)rs[j] > cursor) Emit(cursor, (ulong)rs[j] - 1UL, singles, cidrs, r);
                ulong next = (ulong)re[j] + 1UL;
                if (next > cursor) cursor = next;
                if (cursor > hi) break;
            }
            if (cursor <= hi) Emit(cursor, hi, singles, cidrs, r);

            if (r.EntriesAdded == before) r.CidrsDropped++; else r.CidrsSplit++;
        }

        return r;
    }

    static string FormatIp(uint ip)
    {
        char[] buf = new char[16];
        int p = 0;
        p = WriteOctet(buf, p, (ip >> 24) & 255); buf[p++] = '.';
        p = WriteOctet(buf, p, (ip >> 16) & 255); buf[p++] = '.';
        p = WriteOctet(buf, p, (ip >> 8) & 255);  buf[p++] = '.';
        p = WriteOctet(buf, p, ip & 255);
        return new string(buf, 0, p);
    }

    // Collapses a prefix list into the minimal equivalent set, in ascending order. Used by
    // -RefreshCarveouts: routing data publishes thousands of overlapping announcements.
    public static string[] CollapsePrefixes(string content)
    {
        uint[] noBogon = new uint[0];
        var singles = new HashSet<uint>();
        var cidrs = new HashSet<string>();
        AddContent(content, singles, cidrs, noBogon, noBogon);

        var ranges = BuildRanges(singles, cidrs);
        var result = new List<string>();

        for (int i = 0; i < ranges.Count; i++)
        {
            ulong lo = ranges.Start[i], hi = ranges.End[i];

            while (lo <= hi)
            {
                int bits = 32;
                while (bits > 0)
                {
                    ulong size = 1UL << (32 - (bits - 1));
                    if ((lo % size) != 0UL) break;
                    if (lo + size - 1UL > hi) break;
                    bits--;
                }

                result.Add(bits == 32 ? FormatIp((uint)lo) : FormatCidr((uint)lo, bits));
                lo += 1UL << (32 - bits);
            }
        }

        return result.ToArray();
    }

    static int WriteOctet(char[] buf, int pos, uint v)
    {
        if (v >= 100) { buf[pos++] = (char)('0' + v / 100); buf[pos++] = (char)('0' + (v / 10) % 10); }
        else if (v >= 10) { buf[pos++] = (char)('0' + v / 10); }
        buf[pos++] = (char)('0' + v % 10);
        return pos;
    }

    // Writes the whole blocklist in one streamed pass so we never materialize millions of strings.
    // Header first (the shard's reload detector requires it), then singles, then CIDRs. LF line endings.
    public static void Write(string path, string header, HashSet<uint> singles, HashSet<string> cidrs)
    {
        using (var w = new StreamWriter(path, false, new UTF8Encoding(false), 1 << 20))
        {
            w.Write(header); w.Write('\n');

            char[] buf = new char[20];
            foreach (uint v in singles)
            {
                int p = 0;
                p = WriteOctet(buf, p, (v >> 24) & 255); buf[p++] = '.';
                p = WriteOctet(buf, p, (v >> 16) & 255); buf[p++] = '.';
                p = WriteOctet(buf, p, (v >> 8) & 255);  buf[p++] = '.';
                p = WriteOctet(buf, p, v & 255);         buf[p++] = '\n';
                w.Write(buf, 0, p);
            }

            foreach (string c in cidrs) { w.Write(c); w.Write('\n'); }
        }
    }
}
'@

# ---------------------------------------------------------------------------------------------------------
# Feed set -- thin, non-overlapping. See .DESCRIPTION for why each is kept and what was dropped as redundant.
# romainmarcoux's "full" set is sharded; only aa..ad carry data today (ae.. are empty placeholders).
# A 404/empty shard is skipped, so extend this list if upstream grows the shard count.
# ---------------------------------------------------------------------------------------------------------
$rmBase   = 'https://raw.githubusercontent.com/romainmarcoux/malicious-ip/main/full-300k-'
$rmShards = @('aa','ab','ac','ad') | ForEach-Object { $rmBase + $_ + '.txt' }

$AllFeeds = @(
    [pscustomobject]@{ Name = 'bitwire-it';     Urls = @('https://raw.githubusercontent.com/bitwire-it/ipblocklist/main/inbound.txt') }
    [pscustomobject]@{ Name = 'romainmarcoux';  Urls = $rmShards }
    [pscustomobject]@{ Name = 'sentinel-turris';Urls = @('https://view.sentinel.turris.cz/greylist-data/greylist-latest.csv') }
    [pscustomobject]@{ Name = 'firehol-level1'; Urls = @('https://raw.githubusercontent.com/firehol/blocklist-ipsets/master/firehol_level1.netset') }
)

# Anonymizer / relay lists subtracted only when -ExcludeAnonymizers is set (Tor exits, open proxies, VPN tunnels).
$AnonFeeds = @(
    'https://raw.githubusercontent.com/borestad/firehol-mirror/refs/heads/main/tor_exits.ipset'
    'https://raw.githubusercontent.com/borestad/firehol-mirror/refs/heads/main/sslproxies_7d.ipset'
    'https://raw.githubusercontent.com/borestad/firehol-mirror/refs/heads/main/socks_proxy_7d.ipset'
    'https://raw.githubusercontent.com/ShadowWhisperer/IPs/master/Lists/Tunnels'
)

if ($Feeds) {
    $AllFeeds = $AllFeeds | Where-Object { $Feeds -contains $_.Name }
    if (-not $AllFeeds) { throw "No feeds matched -Feeds." }
}

# ---------------------------------------------------------------------------------------------------------
# Reserved / bogon ranges -- never valid attacker SOURCE IPs; always filtered. Built once as uint32 arrays.
# ---------------------------------------------------------------------------------------------------------
function ConvertTo-IPv4UInt {
    param([string]$s)
    $a = $s.Split('.')
    if ($a.Length -ne 4) { return $null }
    $v = [uint32]0
    foreach ($o in $a) {
        $n = 0
        if (-not [int]::TryParse($o, [ref]$n) -or $n -lt 0 -or $n -gt 255) { return $null }
        $v = ($v -shl 8) -bor [uint32]$n
    }
    return $v
}

$bogonCidrs = '0.0.0.0/8','10.0.0.0/8','100.64.0.0/10','127.0.0.0/8','169.254.0.0/16','172.16.0.0/12',
              '192.0.0.0/24','192.0.2.0/24','192.168.0.0/16','198.18.0.0/15','198.51.100.0/24',
              '203.0.113.0/24','224.0.0.0/3'   # 224/3 covers multicast + reserved + 255.255.255.255
$bogStart = [System.Collections.Generic.List[uint32]]::new()
$bogEnd   = [System.Collections.Generic.List[uint32]]::new()
foreach ($c in $bogonCidrs) {
    $p = $c.Split('/'); $base = ConvertTo-IPv4UInt $p[0]; $bits = [int]$p[1]
    $size = [uint32]([Math]::Pow(2, 32 - $bits))
    $bogStart.Add($base); $bogEnd.Add([uint32]($base + $size - 1))
}
$bogStart = $bogStart.ToArray(); $bogEnd = $bogEnd.ToArray()

# ---------------------------------------------------------------------------------------------------------
# Streaming download with a live progress bar (Write-Progress) so large feeds show real byte progress.
# ---------------------------------------------------------------------------------------------------------
function Get-Url {
    param([string]$Url, [string]$Label)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    $req.UserAgent = $UA; $req.Timeout = 120000; $req.ReadWriteTimeout = 120000
    $resp = $req.GetResponse()
    try {
        $total  = $resp.ContentLength
        $stream = $resp.GetResponseStream()
        $ms  = New-Object System.IO.MemoryStream
        $buf = New-Object byte[] (1MB)
        $read = 0; $lastReport = 0
        while (($n = $stream.Read($buf, 0, $buf.Length)) -gt 0) {
            $ms.Write($buf, 0, $n); $read += $n
            if ($read - $lastReport -ge 2MB) {
                $lastReport = $read
                if ($total -gt 0) {
                    Write-Progress -Activity ("Downloading {0}" -f $Label) -PercentComplete ([int](100 * $read / $total)) `
                        -Status ("{0:N1} / {1:N1} MB" -f ($read / 1MB), ($total / 1MB))
                } else {
                    Write-Progress -Activity ("Downloading {0}" -f $Label) -Status ("{0:N1} MB" -f ($read / 1MB))
                }
            }
        }
        Write-Progress -Activity ("Downloading {0}" -f $Label) -Completed
        return [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
    }
    finally { $resp.Close() }
}

# ---------------------------------------------------------------------------------------------------------
# Refresh carve-out data from routing announcements. Runs here because it needs Get-Url and the compiled
# collapser. A failure leaves the existing file alone rather than truncating a working carve-out.
# ---------------------------------------------------------------------------------------------------------
if ($AddCarveout) {
    $path = Join-Path $ConfigDir ("ip-allowlist-{0}.txt" -f $AddCarveout)
    $prefixes = Get-CarveoutPrefixes -CarveoutAsn $Asn

    Write-AllowlistFile -Path $path -Lines (@(Get-CarveoutHeader -Name $AddCarveout -CarveoutAsn $Asn) + $prefixes)
    Write-Host ("Created {0} carve-out from AS{1}: {2} prefixes -> {3}" -f $AddCarveout, $Asn, @($prefixes).Count, $path)

    if ($AllowPaths -notcontains $path) { $AllowPaths += $path }
}

if ($RefreshCarveouts) {
    foreach ($path in $AllowPaths) {
        $carveoutAsn = Get-CarveoutAsn -Path $path
        if ($carveoutAsn -le 0) { continue }   # hand-written allowlist, not ours to rewrite

        try {
            $name = [IO.Path]::GetFileNameWithoutExtension($path) -replace '^ip-allowlist-', ''
            $prefixes = Get-CarveoutPrefixes -CarveoutAsn $carveoutAsn
            Write-AllowlistFile -Path $path -Lines (@(Get-CarveoutHeader -Name $name -CarveoutAsn $carveoutAsn) + $prefixes)
            Write-Host ("Refreshed {0} carve-out from AS{1}: {2} prefixes" -f $name, $carveoutAsn, @($prefixes).Count)
        }
        catch {
            Write-Warning ("AS{0}: refresh failed ({1}) -- keeping the existing carve-out" -f $carveoutAsn, $_.Exception.Message)
        }
    }
}

# ---------------------------------------------------------------------------------------------------------
# Collect every kept feed into ONE global set, timing each phase.
# ---------------------------------------------------------------------------------------------------------
$singles = [System.Collections.Generic.HashSet[uint32]]::new()
$cidrs   = [System.Collections.Generic.HashSet[string]]::new()
$feedCount = 0

foreach ($feed in $AllFeeds) {
    $before = $singles.Count + $cidrs.Count
    $ok = $false
    foreach ($url in $feed.Urls) {
        try {
            $dlSw = [System.Diagnostics.Stopwatch]::StartNew()
            $content = Get-Url -Url $url -Label $feed.Name
            $dlSw.Stop()
            $mb = [Math]::Round($content.Length / 1MB, 1)

            $pSw = [System.Diagnostics.Stopwatch]::StartNew()
            [void][BlocklistExporter]::AddContent($content, $singles, $cidrs, $bogStart, $bogEnd)
            $pSw.Stop()
            Write-Host ("  [dl {0,6:N1}s / parse {1,5:N1}s] {2}" -f $dlSw.Elapsed.TotalSeconds, $pSw.Elapsed.TotalSeconds, ("{0} ({1} MB)" -f $feed.Name, $mb))
            $ok = $true
        }
        catch { Write-Warning ("{0}: {1} -- skipping shard ({2})" -f $feed.Name, $url, $_.Exception.Message) }
    }
    if ($ok) {
        $feedCount++
        $delta = ($singles.Count + $cidrs.Count) - $before
        Write-Host ("{0,-16} +{1,8} new  (running total {2} ip / {3} cidr)`n" -f $feed.Name, $delta, $singles.Count, $cidrs.Count)
    }
    else { Write-Warning ("{0}: all sources failed -- skipping" -f $feed.Name) }
}

# ---------------------------------------------------------------------------------------------------------
# Subtraction pass: the allowlist file, plus the anonymizer feeds when -ExcludeAnonymizers is set. Both are
# "never block these", so they share one set and one range-correct removal -- which is also the fix for the
# old anonymizer path, where CIDR entries were parsed and then never actually subtracted.
#
# Bogon filtering is deliberately NOT applied here: it exists to keep junk OUT of the blocklist, and running
# it over subtractive input would quietly discard exemptions instead (e.g. a shard exempting its own LAN).
# ---------------------------------------------------------------------------------------------------------
$allowSingles = [System.Collections.Generic.HashSet[uint32]]::new()
$allowCidrs   = [System.Collections.Generic.HashSet[string]]::new()
$noBogon      = [uint32[]]::new(0)

foreach ($p in $AllowPaths) {
    try {
        $allowText = Get-Content -LiteralPath $p -Raw -ErrorAction Stop
        if (-not $allowText) { continue }

        $before = $allowSingles.Count + $allowCidrs.Count
        [void][BlocklistExporter]::AddContent($allowText, $allowSingles, $allowCidrs, $noBogon, $noBogon)
        Write-Host ("  allowlist {0,-28} +{1} entr(ies)" -f (Split-Path -Leaf $p), (($allowSingles.Count + $allowCidrs.Count) - $before))
    }
    catch { Write-Warning ("Could not read allowlist {0}: {1}" -f $p, $_.Exception.Message) }
}

if ($ExcludeAnonymizers) {
    foreach ($url in $AnonFeeds) {
        try { [void][BlocklistExporter]::AddContent((Get-Url -Url $url -Label 'anonymizers'), $allowSingles, $allowCidrs, $noBogon, $noBogon) }
        catch { Write-Warning ("anonymizer list {0}: {1}" -f $url, $_.Exception.Message) }
    }
}

$allowCount = $allowSingles.Count + $allowCidrs.Count
if ($allowCount -gt 0) {
    $aSw = [System.Diagnostics.Stopwatch]::StartNew()
    $ranges = [BlocklistExporter]::BuildRanges($allowSingles, $allowCidrs)
    $res = [BlocklistExporter]::ApplyAllowlist($singles, $cidrs, $ranges)
    $aSw.Stop()

    Write-Host ("Allowlist: {0} entr(ies) -> {1} ranges; removed {2} IPs, dropped {3} CIDRs, split {4} into {5} ({6:N1}s)" -f `
        $allowCount, $ranges.Count, $res.SinglesRemoved, $res.CidrsDropped, $res.CidrsSplit, $res.EntriesAdded, $aSw.Elapsed.TotalSeconds)
}

$total = $singles.Count + $cidrs.Count
Write-Host ("Merged {0} feed(s): {1} unique single IPs + {2} unique CIDRs (bogon-filtered) in {3:N1}s." -f `
    $feedCount, $singles.Count, $cidrs.Count, $totalSw.Elapsed.TotalSeconds)

if ($DryRun) {
    Write-Host ("DRY RUN: nothing written (would have written {0} entries to {1})." -f $total, $OutFile)
    return
}

# A partial feed outage must not silently shrink the shard's blocklist to nothing; keep the last good file.
if ($total -eq 0) { throw "No entries parsed -- refusing to overwrite '$OutFile' with an empty list." }

# ---------------------------------------------------------------------------------------------------------
# Write to a .tmp sibling and swap it into place, so the shard (which reads the whole file on a change)
# never observes a half-written list. One rename does it whether or not a list is already there.
# ---------------------------------------------------------------------------------------------------------
$outDir = Split-Path -Parent $OutFile
if ($outDir -and -not (Test-Path -LiteralPath $outDir -PathType Container)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

# InvariantCulture: ':' is the culture-defined time separator in a custom format string, and the
# header is a machine-read marker the shard compares verbatim.
$generated = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
# The shard's header reader is token-based and ignores tokens it does not know, so `allow=` is additive --
# it is here so an operator can tell from the file alone whether a carve-out was in effect when it was built.
$header = "# modernuo-blocklist generated=$generated count=$total ipv4=$($singles.Count) cidr=$($cidrs.Count) feeds=$feedCount allow=$allowCount"

$tmp = $OutFile + '.tmp'
$wSw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    [BlocklistExporter]::Write($tmp, $header, $singles, $cidrs)
    # One atomic rename over the destination on every platform: MoveFileEx REPLACE_EXISTING on
    # Windows, rename(2) on Linux and macOS.
    [IO.File]::Move($tmp, $OutFile, $true)
}
finally {
    # Never leave a partial .tmp next to a live blocklist for the next run to trip over.
    if (Test-Path -LiteralPath $tmp -PathType Leaf) { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue }
}
$wSw.Stop()

$sizeMb = [Math]::Round((Get-Item -LiteralPath $OutFile).Length / 1MB, 1)
Write-Host ("`nWrote {0} entries ({1} MB) to {2} in {3:N1}s (total {4:N1}s). generated={5}" -f `
    $total, $sizeMb, $OutFile, $wSw.Elapsed.TotalSeconds, $totalSw.Elapsed.TotalSeconds, $generated)
Write-Host "The shard picks this up on its next reloadInterval poll; no restart needed."
