#requires -Version 5.1
<#
.SYNOPSIS
    Downloads a small, non-overlapping set of public IP threat feeds and writes them to a single
    ModernUO blocklist file — merged, de-duplicated and bogon-filtered.

.DESCRIPTION
    This is the producer half of ModernUO's in-app blocklist gate. It fetches a deliberately THIN feed
    set, merges every source into one global set, drops duplicates and reserved/bogon addresses, then
    writes the result to a plain text file that the shard reads via `file` in
    Configuration/blocklist.json. Nothing is installed and no credentials are needed — the output is just
    a text file, so this can run on any machine that can reach the shard's Distribution folder.

    It writes the file the shard's `BlocklistFilter` demand-pages against. IPs that actually connect are
    promoted to CrowdSec / the OS firewall by the shard; the OS firewall never has to hold millions of
    entries, which is exactly the scale it cannot handle on Windows.

    Inclusion principle: any category of IP used in OTHER attacks that could plausibly be turned against a
    game server should be blocked — compromised hosts, botnets, scanners, spam / DDoS-as-a-service bots,
    open proxies and Tor relays. That whole surface is already covered by the anchor feed `bitwire-it`,
    which is itself a 91-source aggregator (it folds in spamhaus, ipsum, firehol-level2, blocklist-de,
    dshield, emergingthreats, binarydefense, cins-army, bruteforceblocker, greensnow, vxvault, ThreatFox,
    StopForumSpam/sblam, Tor, open-proxy and C2 lists). So the inclusive posture lives in the base layer,
    and every one of those standalone feeds is dropped as pure redundancy. Only the feeds bitwire does NOT
    already carry are kept on top of it:

        bitwire-it        2h-refreshed 91-source aggregate (compromised hosts, botnets, scanners, spam
                          bots, Tor/open-proxy abuse relays, ThreatFox C2) — the broad base layer.
        romainmarcoux     ~130k fresh attacker IPs bitwire's snapshot lags on (high-churn feed).
        sentinel-turris   ~800 unique honeypot probers (Turris greylist) not in bitwire.
        firehol-level1    hijacked/reputation NETBLOCKS (spamhaus DROP-style) — bogon-filtered.

    The only category deliberately held back is commercial VPN exit endpoints, which could block a legit
    player — and those are barely present here anyway (bitwire is ~5% of VPN-tunnel lists). If you ever want
    to protect VPN/Tor players, pass -ExcludeAnonymizers to subtract Tor/open-proxy/VPN IPs from the output.

    OUTPUT FORMAT (must stay in sync with UOContent/Misc/Blocklist/BlocklistFile.cs):
        Line 1 is a header comment carrying the version markers, e.g.
            # modernuo-blocklist generated=2026-07-25T18:03:11Z count=3914022 ipv4=3901188 cidr=12834
        The shard polls `reloadInterval` and reloads when the file mtime AND `generated=` change,
        so the header is REQUIRED — without it the shard loads once and never picks up a new file.
        Every following line is one entry: a bare IPv4/IPv6 address or a CIDR (`1.2.3.0/24`). Blank lines
        and lines starting with `#` or `;` are ignored. Order does not matter; the shard sorts and
        coalesces on load. The feeds used here are IPv4-only, but the shard parses IPv6 lines too.

    The file is written to a `.tmp` sibling and swapped into place atomically, so the shard never reads a
    half-written list — it either sees the previous version or the new one, whole.

    Performance: bitwire alone is ~4M lines. Parsing/validating/bogon-filtering that in interpreted
    PowerShell is the slow part (minutes), so the hot loop is compiled once via Add-Type (C#) — it runs in
    ~1s. Downloads stream with a live Write-Progress bar; every phase prints its own elapsed time so you can
    see exactly where the wall-clock goes.

    Runs on Windows PowerShell 5.1 and on PowerShell 7 for Windows, Linux and macOS. Schedule it with
    Task Scheduler, cron, or a systemd timer.

    Every run rewrites the whole file, so an IP that drops off the feeds stops being blocked on the next
    run — there is no TTL to tune. Calling it is idempotent: if the list on disk is younger than
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
    machines and reboots — there is no separate state file. Nothing is downloaded when the check trips.
    Accepts `90s`, `45m`, `2h`, `2.5h`, `1d`, or a bare number of hours. Use `0` to disable the check.
    Match this to how often you actually want fresh data: the anchor feed only refreshes every 2h, so
    running more often than that costs bandwidth and gains nothing.

.PARAMETER Force
    Run regardless of how recently the blocklist was generated (bypasses -MinInterval).

.PARAMETER Feeds
    Which feeds to include (by Name). Default: all of them.

.PARAMETER ExcludeAnonymizers
    Also download Tor-exit / open-proxy / VPN-tunnel lists and SUBTRACT those IPs from the output. Off by
    default — for a game server, Tor/open-proxy relays are attack infrastructure you want to block. Turn
    this on only if you need to keep VPN/Tor players reachable.

.PARAMETER DryRun
    Download + parse + merge + count only. Writes nothing.

.EXAMPLE
    .\Export-IpBlocklist.ps1 -DryRun

.EXAMPLE
    # Safe to call as often as you like — it no-ops unless the list is older than 2h.
    .\Export-IpBlocklist.ps1 -DistributionPath 'C:\Shard\Distribution'

.EXAMPLE
    .\Export-IpBlocklist.ps1 -OutFile 'D:\shared\ip-blocklist.txt' -ExcludeAnonymizers

.EXAMPLE
    # Regenerate right now, ignoring the cooldown.
    .\Export-IpBlocklist.ps1 -DistributionPath 'C:\Shard\Distribution' -Force

.EXAMPLE
    # Linux/macOS, e.g. from cron:
    pwsh -File /opt/modernuo/Export-IpBlocklist.ps1 -DistributionPath /opt/modernuo/Distribution

.NOTES
    Feeds are aggressive-but-low-FP for a game server (attacker / botnet / compromised / abuse-relay SOURCE
    IPs). Reserved/bogon space (0/8, 10/8, 127/8, RFC1918, multicast, etc.) is always filtered out — this
    matters because firehol-level1 ships bogon netblocks that would otherwise block private/reserved ranges.
#>
[CmdletBinding()]
param(
    [string]   $DistributionPath,
    [string]   $OutFile,
    [string]   $MinInterval = '2h',
    [string[]] $Feeds,
    [switch]   $ExcludeAnonymizers,
    [switch]   $Force,
    [switch]   $DryRun
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell (.NET Framework) still defaults to SSL3/TLS1 and needs this. PowerShell 7 on any
# platform negotiates TLS 1.2/1.3 on its own, and ServicePointManager is a legacy no-op there.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}

$UA = 'ModernUO-Blocklist-Export'
$totalSw = [System.Diagnostics.Stopwatch]::StartNew()

# Default location under the Distribution folder. Keep in sync with BlocklistSettings.File.
# Joined a segment at a time (never a literal 'a\b') so the separator is right on Linux and macOS.
$DefaultPathSegments = @('Configuration', 'ip-blocklist.txt')

# File.Move(source, dest, overwrite) is .NET Core only. Where it exists it is the portable atomic
# replace; Windows PowerShell 5.1 falls back to File.Replace. Probed once, used at the swap below.
$MoveCanOverwrite = [bool][IO.File].GetMethod('Move', [Type[]]@([string], [string], [bool]))

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
    # One segment per Join-Path: the multi-argument form is PowerShell 6+ only, and this stays 5.1-safe.
    $OutFile = $DistributionPath
    foreach ($segment in $DefaultPathSegments) {
        $OutFile = Join-Path $OutFile $segment
    }
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
if (-not $Force -and $minAge -gt [TimeSpan]::Zero) {
    $existing = Get-BlocklistAge -Path $OutFile
    if ($existing) {
        # A negative age means the stamp is in the future (clock skew, or a file from another host). Treat it
        # as fresh: refusing to run is the recoverable failure, hammering the feeds on every tick is not.
        if ($existing.Age -lt $minAge) {
            $agoText = if ($existing.Age -lt [TimeSpan]::Zero) { 'in the future — check the clock' } else { ("{0:N1}h ago" -f $existing.Age.TotalHours) }
            Write-Host ("Blocklist at {0} was generated {1} ({2}={3}); newer than -MinInterval {4}." -f `
                $OutFile, $agoText, $existing.Source, $existing.Stamp, $MinInterval)
            Write-Host "Nothing downloaded. Pass -Force to regenerate now, or lower -MinInterval."
            return
        }
    }
}

# ---------------------------------------------------------------------------------------------------------
# Compiled hot loop. Interpreted PowerShell chokes on bitwire's ~4M lines; this parses + validates + bogon-
# filters + de-dupes in one compiled pass, and writes the final file directly (no 4M-element PS pipelines).
# Kept to C# 5 syntax so it also compiles under Windows PowerShell 5.1's .NET Framework compiler.
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
# Feed set — thin, non-overlapping. See .DESCRIPTION for why each is kept and what was dropped as redundant.
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
# Reserved / bogon ranges — never valid attacker SOURCE IPs; always filtered. Built once as uint32 arrays.
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
        catch { Write-Warning ("{0}: {1} — skipping shard ({2})" -f $feed.Name, $url, $_.Exception.Message) }
    }
    if ($ok) {
        $feedCount++
        $delta = ($singles.Count + $cidrs.Count) - $before
        Write-Host ("{0,-16} +{1,8} new  (running total {2} ip / {3} cidr)`n" -f $feed.Name, $delta, $singles.Count, $cidrs.Count)
    }
    else { Write-Warning ("{0}: all sources failed — skipping" -f $feed.Name) }
}

# ---------------------------------------------------------------------------------------------------------
# Optional: subtract Tor / open-proxy / VPN IPs.
# ---------------------------------------------------------------------------------------------------------
if ($ExcludeAnonymizers) {
    $anon = [System.Collections.Generic.HashSet[uint32]]::new()
    $anonCidr = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($url in $AnonFeeds) {
        try { [void][BlocklistExporter]::AddContent((Get-Url -Url $url -Label 'anonymizers'), $anon, $anonCidr, $bogStart, $bogEnd) }
        catch { Write-Warning ("anonymizer list {0}: {1}" -f $url, $_.Exception.Message) }
    }
    $removed = 0
    foreach ($ip in @($anon)) { if ($singles.Remove($ip)) { $removed++ } }
    Write-Host ("ExcludeAnonymizers: removed {0} Tor/proxy/VPN single IPs" -f $removed)
}

$total = $singles.Count + $cidrs.Count
Write-Host ("Merged {0} feed(s): {1} unique single IPs + {2} unique CIDRs (bogon-filtered) in {3:N1}s." -f `
    $feedCount, $singles.Count, $cidrs.Count, $totalSw.Elapsed.TotalSeconds)

if ($DryRun) {
    Write-Host ("DRY RUN: nothing written (would have written {0} entries to {1})." -f $total, $OutFile)
    return
}

# A partial feed outage must not silently shrink the shard's blocklist to nothing; keep the last good file.
if ($total -eq 0) { throw "No entries parsed — refusing to overwrite '$OutFile' with an empty list." }

# ---------------------------------------------------------------------------------------------------------
# Write to a .tmp sibling and swap it into place, so the shard (which reads the whole file on a change)
# never observes a half-written list. File.Replace is atomic on NTFS; Move covers the first-ever run.
# ---------------------------------------------------------------------------------------------------------
$outDir = Split-Path -Parent $OutFile
if ($outDir -and -not (Test-Path -LiteralPath $outDir -PathType Container)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

# InvariantCulture: ':' is the culture-defined time separator in a custom format string, and the
# header is a machine-read marker the shard compares verbatim.
$generated = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
$header = "# modernuo-blocklist generated=$generated count=$total ipv4=$($singles.Count) cidr=$($cidrs.Count) feeds=$feedCount"

$tmp = $OutFile + '.tmp'
$wSw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    [BlocklistExporter]::Write($tmp, $header, $singles, $cidrs)
    if (-not (Test-Path -LiteralPath $OutFile -PathType Leaf)) {
        [IO.File]::Move($tmp, $OutFile)
    }
    elseif ($MoveCanOverwrite) {
        # .NET Core / PowerShell 7: one atomic rename over the destination on every platform
        # (MoveFileEx REPLACE_EXISTING on Windows, rename(2) on Linux/macOS).
        [IO.File]::Move($tmp, $OutFile, $true)
    }
    else {
        # Windows PowerShell 5.1 has no 3-argument Move; File.Replace is the atomic equivalent there.
        # [NullString]::Value, not $null: PowerShell marshals $null to "" for string parameters, and
        # File.Replace rejects an empty backup path. Null means "no backup copy" — the point of using it.
        [IO.File]::Replace($tmp, $OutFile, [System.Management.Automation.Language.NullString]::Value)
    }
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
