# IP Bans, Blocklists and Allowlists

How a shard decides to refuse a connection, how it contributes bans to an external bouncer, and how an
operator exempts someone who was caught by mistake.

If you are here because **a player cannot connect**, skip to [Unblocking a player](#unblocking-a-player).

## The shape of it

Two independent questions, deliberately separated:

| Question | Answered by | Effect |
|---|---|---|
| Refuse this connection? | `IConnectionFilter` implementations, at accept | The socket is dropped |
| Tell the outside world about it? | `BanChannel` → `IBanReporter` implementations | CrowdSec, and from there an OS bouncer |

`BanChannel` **never enforces** and filters **never report on each other's behalf**. Enforcement that
outlives the process belongs to the OS bouncer; the shard only contributes.

### Refusing a connection

Filters are consulted in registration order, first denial wins:

| Filter | Source | Scope |
|---|---|---|
| `firewall` | `Configuration/firewall.json`, mutable in-game | Admin-curated, permanent |
| `blocklist` | `Configuration/ip-blocklist.txt` (millions of entries, opt-in) | Reputation feeds |
| `auto-denylist` | In-memory, 15 min | What this shard just caught misbehaving |

### Contributing a ban

`BanChannel.Report(address, ttl, reason)` → `BanExemptions.IsExempt` → if not exempt, fan out to every
reporter (`crowdsec`, `auto-denylist`).

### Allowlists

Two, with different authority:

| List | Source | Revocable? | Covers |
|---|---|---|---|
| `ManualAllowlist` | every `ip-allowlist*.txt` (opt-in) | No — an operator said so | Blocking **and** escalation |
| `LoginAllowlist` | Earned by authenticating, 90-day TTL | Yes — 10 strikes/hour | Blocking **and** escalation |

Both are consulted **only after the blocklist has already matched**, so a normal accept — the one an
attacker is trying to flood — pays nothing for them. The accept gate itself is deliberately allowlist-free:
a whitelist there could only turn a deny into an allow at the cost of a lookup on every accept, the
attacker's included.

## Unblocking a player

### 1. Find out what is actually blocking them

```bash
# In the reputation blocklist?
grep -x "203.0.113.42" Distribution/Configuration/ip-blocklist.txt

# A live external decision? (this is what survives a restart)
cscli decisions list --ip 203.0.113.42

# Admin-curated?
grep 203.0.113.42 Distribution/Configuration/firewall.json
```

If none of those match, they may be inside a **CIDR** in the blocklist, or held by the in-memory
`auto-denylist` — that one is not queryable and expires on its own within 15 minutes.

### 2. Add them to the allowlist

Set `"enabled": true` in `Configuration/ip-allowlist.json` first — it is off by default, so a shard that
has never written a carve-out does not poll for one. The shard logs a warning at startup if allowlist files
are present while the flag is off.

Then one entry per line in `Distribution/Configuration/ip-allowlist.txt` — a bare address or a CIDR. This
file is yours; the generator creates it once and never rewrites it.

```
203.0.113.42        # shard owner, listed via a shared upstream address
198.51.100.0/24     # a whole range if the ISP rotates within it
```

With the flag on, the shard reloads within `reloadInterval` (60s default). **No restart, and no need to
re-run the generator.** From that point the address is neither blocked nor contributed. With the flag off
the generator still subtracts the file at generation time, so the address stops being *blocked* — but a
behavioural detection can still contribute it, which is the case the flag exists to cover.

### 3. Clear any ban that already exists

A config change cannot retract a ban that has already left the building:

```bash
cscli decisions delete -i 203.0.113.42
```

### 4. If it keeps coming back

The shard is no longer contributing them, so a recurring ban is coming from CrowdSec's own sources (the
community blocklist, another watcher). Allowlist it there too:

```bash
cscli allowlists create shard-staff -d "Known-good player addresses"
cscli allowlists add shard-staff 203.0.113.42
```

### What will NOT work

- **Deleting the CrowdSec decision alone.** If the address is still in `ip-blocklist.txt` and not
  allowlisted, the next connection re-reports it within `promoteSuppression` (60s).
- **Editing `ip-blocklist.txt` by hand.** The next generator run rewrites the whole file.
- **`cscli allowlists` alone.** That is the enforcement layer. The shard's own accept gate sits upstream of
  it and will still refuse the connection.

## Why entries appear that should not

Reputation feeds list shared consumer address space constantly. On CGNAT one public address fronts many
subscribers **at the same time**, so a single abusive customer gets the address listed and everyone else
behind it is blocked with them — and where leases rotate, a listing says little about whoever holds the
address now. This is near-universal on mobile carriers, satellite (Starlink) and WISPs, and common on
fixed-line broadband outside North America.

A **carve-out** exempts a whole network. **None ship with ModernUO** — which providers to exempt depends on
where your players actually are, and a carve-out names a real network, so you build the ones you need:

```powershell
# Exempt a CGNAT provider whose players keep getting listed
.\Export-IpBlocklist.ps1 -AddCarveout starlink -Asn 14593

# Later: bring every carve-out up to date with what those networks currently announce
.\Export-IpBlocklist.ps1 -RefreshCarveouts
```

That writes `ip-allowlist-starlink.txt` beside the blocklist, and every `ip-allowlist*.txt` there is
subtracted by the generator with no config edit. For the shard to read them too — which is what also stops
a carve-out address being *contributed* by a behavioural detection — set `enabled` in `ip-allowlist.json`;
it is off by default so no shard polls for files it never wrote. Starlink costs about 0.1% of the list.
Blank a file (keep the file) to reputation-block that network again; delete it to drop the carve-out.

Carve-out files carry an `asn=` marker in their header, which is how `-RefreshCarveouts` finds them. A
hand-written allowlist has no marker and is never rewritten.

**Do you need one?** If players report being blocked and they are on satellite, mobile, or an ISP short on
IPv4, probably yes. Find the ASN by looking up an address the network hands out on any public BGP lookup.

Prefixes come from **announcements, not ownership records**. Registry data disagrees with what is actually
routed and silently caps result sets: ARIN whois returns at most 256 rows and gives per-customer /24s, and
`206.83.96.0/19` reads as APNIC in RDAP even though `206.83.96/21` is announced by Starlink.

## Behavioural detection

Verdicts the shard reaches by watching a connection, rather than by consulting a list:

| Reason | Trigger |
|---|---|
| `rate-limit` | Too many connection attempts in the limiter's window |
| `silent-connect` | Reaped after `ConnectingSocketIdleLimit` (5s) having sent **zero bytes** |
| `invalid-seed` | Opened with a zero seed, which no real client sends |
| `foreign-protocol` | Positively identified as HTTP, TLS or SSH |

`BanReasons.IsBehavioral` gates two things: only these may be exempted, and only these enter the
`auto-denylist`. It is an **opt-in list**, not "everything except `manual`" — a reason added later escalates
normally rather than silently inheriting an exemption. `manual` is never exempt and never auto-denied.

Escalation is **immediate**, on the first detection: a 15-minute local hold plus a `badConnectDuration`
(4h) contribution. There is no N-connection threshold; the strike counter governs only revoking a
`LoginAllowlist` entry.

The local hold runs 15 minutes from the **first** detection and is never extended by later ones, so an
address that keeps trying is released on schedule rather than held indefinitely. It does not get a free
run: the rate limiter sits *ahead* of the connection filters, so a flooder is re-reported and re-held on
its next attempt. Not refreshing is what keeps the holds in expiry order, which is what makes retiring
lapsed ones cost the number expiring rather than the number held.

### What is deliberately NOT detected

**Do not add rules based on arrival framing.** TCP has no message boundaries, so the network, the OS or a
middlebox can split the opening bytes anywhere regardless of what the client sent. A rule of the form
"these bytes must arrive together" is broken by construction and drops real players on poor links. This has
been tried and reverted before.

**Do not treat unreadable payloads as hostile.** A legitimate client with encryption enabled when the shard
expects none sends a structurally perfect connection whose payload is noise — `LoginEncryption.ClientDecrypt`
is a byte-for-byte stream XOR, so it preserves length exactly while destroying content. This is why
detection asks "is this positively some *other* protocol?" rather than "is this a good UO client?": however
misconfigured a UO client is, it never sends `GET / HTTP/1.1`.

**Timeouts are keyed on bytes-received, not elapsed time.** A connection that sent *something* and ran out
of time is far more likely a slow link than an attack. Banning those produces the worst failure mode
available: the player retries, trips the rate limiter, and compounds a bad connection into hours of being
firewalled off. Shortening the 5s handshake window has been tried and broke real players.

## Known limits

- **An allowlist cannot bootstrap.** A `LoginAllowlist` entry is only earned by getting in, so it can never
  repair an existing false positive, and it is weakest on rotating CGNAT — a player whose lease moved is a
  stranger again. `ManualAllowlist` is the fix for that, which is why it is manual — and opt-in, via
  `ip-allowlist.json`.
- **A never-logged-in player on a shared address can still be caught**, for up to `badConnectDuration`, if a
  co-tenant misbehaves. Accepted: it is 4h and self-healing. The cheapest lever is `badConnectDuration`.
- **`MaxConnections` (4096) is a hard ceiling.** The accept gate runs *after* the kernel completed the TCP
  handshake, so a blocklist match saves the socket setup and the `NetState` slot but never the connection
  itself. Only an upstream L4 proxy or edge scrubbing moves that cost off the shard.
- **The `auto-denylist` stops tracking at `maxEntries`.** Past it a detection still disconnects the
  connection, but the address is not held, so it pays full detection cost on every reconnect instead of a
  cheap accept-gate deny. The default is sized for the 50k–250k distinct-source floods seen in practice; a
  flood past it wants upstream scrubbing rather than a larger cap, which only buys a longer on-loop scan.

## Configuration

| File | Controls |
|---|---|
| `bans.json` | `reportRateLimitTrips`, `autoBanDuration`, `reportBadConnects`, `badConnectDuration` |
| `blocklist.json` | `enabled` (default `false`), `file`, `reloadInterval`, `reportHits`, `banDuration`, `promoteSuppression` |
| `ip-allowlist.json` | `enabled` (default `false`), `files` (wildcards allowed), `reloadInterval` |
| `login-allowlist.json` | `enabled`, `file`, `ttl`, `flushInterval`, `escalateAfterStrikes`, `strikeWindow` |
| `auto-denylist.json` | `enabled`, `duration`, `maxEntries` (default `324,449` — sized for the floods seen in practice; see the remark on the setting before raising it) |
| `crowdsec.json` | `lapiUrl`, `machineId`, `password`, `origin`, `manualBanDuration`, `flushInterval`, `maxQueue` |
| `firewall.json` | Admin-curated entries |

A shard fronted by an upstream proxy can disable all of it and register nothing.

## Key files

| File | Role |
|---|---|
| `Projects/Server/Network/IConnectionFilter.cs` | Accept-path gate contract |
| `Projects/Server/Network/ConnectionFilters.cs` | Filter registry + lifecycle |
| `Projects/Server/Network/ForeignProtocol.cs` | Positive identification of non-UO traffic |
| `Projects/Server/Network/Bans/BanChannel.cs` | Contribution fan-out + `IsExempt` seam |
| `Projects/Server/Network/Bans/BanReasons.cs` | Reason slugs + the behavioural opt-in set |
| `Projects/UOContent/Network/BanExemptions.cs` | Combines both allowlists into one answer |
| `Projects/UOContent/Network/Blocklist/BlocklistFilter.cs` | File-sourced blocklist filter |
| `Projects/UOContent/Network/Blocklist/ManualAllowlist.cs` | Operator carve-outs, read from the allowlist files |
| `Projects/UOContent/Network/LoginAllowlist/LoginAllowlist.cs` | Allowlist earned by authenticating |
| `Projects/UOContent/Network/AutoDenylist/AutoDenylist.cs` | Short-lived local hold |
| `Projects/UOContent/Network/CrowdSec/CrowdSecReporter.cs` | LAPI contribution sink |
| `Projects/UOContent/Network/Firewall/Firewall.cs` | Admin-curated firewall set |
| `tools/Export-IpBlocklist.ps1` | Blocklist generator + allowlist subtraction |
