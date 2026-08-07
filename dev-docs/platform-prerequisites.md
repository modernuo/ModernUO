# Platform Prerequisites

OS-level dependencies ModernUO needs at runtime, why each one is required, and what breaks without
it. This page is about software packages, not hardware sizing.

Run `./build-tool --check-prereqs` from the repository root to check the current machine. It prints
the exact install command for the detected distribution.

## What is required

| Dependency | Platform | Why |
|---|---|---|
| .NET 10 Runtime | all | — |
| ICU (`libicuuc`, `libicui18n`) | Linux, macOS | The runtime refuses to start without it; see below |
| tzdata | Linux | Time zone lookups; see below |
| `libdeflate` | all | `LibDeflate.Bindings` |
| `libargon2` | all | `Argon2.Bindings` (password hashing) |
| VC++ Redistributable v14 | Windows | Native bindings |

Not required, despite appearances:

- **zstd** — `ZstdNet` bundles `libzstd` for every RID.
- **liburing** — `IORingGroup` issues `io_uring` syscalls directly. It imports only `libc`,
  `libSystem.dylib`, `kernel32.dll`, `kernelbase.dll` and `ws2_32.dll`.
- **`-dev` / `-devel` packages** — see "Runtime packages only" below.

## Install

```sh
# Debian / Ubuntu   (ICU has no stable package alias, so match it by pattern)
sudo apt-get install -y '^libicu[0-9]+$' libdeflate0 libargon2-1 tzdata

# Fedora / RHEL
sudo dnf install -y libdeflate libargon2 libicu tzdata

# Alpine
apk add --no-cache libdeflate argon2-libs icu-libs tzdata

# macOS
brew install icu4c libdeflate argon2
```

CentOS additionally needs EPEL and CRB:

```sh
sudo dnf install -y epel-release epel-next-release && sudo dnf config-manager --set-enabled crb
```

## Runtime packages only

Only the runtime packages are needed. The `-dev`/`-devel` packages are **not** required.

They used to be, because .NET's `DllImport` probing looks for the unversioned `libfoo.so`, and on
Linux that bare symlink ships only in the development package. The runtime package ships the
versioned SONAME (`libdeflate.so.0`, `libargon2.so.1`). The binding packages now probe the versioned
names as well, so the runtime package is sufficient.

Anything still documenting `libicu-dev` or `libdeflate-dev` as a requirement is out of date.

## ICU

`Directory.Build.props` sets `InvariantGlobalization=false`, so ICU is mandatory. Without it the
runtime does **not** throw — it `FailFast`s:

```
Couldn't find a valid ICU package installed on the system. Please install libicu (or icu-libs)
using your package manager and try again.
```

That is `SIGABRT` (exit 134) and it cannot be caught. Note the process **starts cleanly and aborts
later**, at whatever line first touches a culture, so the crash rarely points at the cause.

### Why invariant mode is not an option

`InvariantGlobalization=true` would remove the ICU dependency, but it changes behaviour in ways that
corrupt data silently. Measured on .NET 10 with the repository's settings:

| Behaviour | With ICU | Invariant mode |
|---|---|---|
| `new CultureInfo("de-DE")` | real culture | succeeds, returns invariant data |
| de-DE decimal separator | `,` | `.` |
| `1234.5` as de-DE | `1.234,5` | `1,234.5` |
| `string.Compare("a", "B", InvariantCulture)` | `-1` (linguistic) | `31` (ordinal) |
| sort `[b, A, a, B]` | `a, A, b, B` | `A, B, a, b` |
| `FindSystemTimeZoneById("Eastern Standard Time")` on Linux | resolves | `TimeZoneNotFoundException` |
| UTF-8 round-trip of non-ASCII | unaffected | unaffected |

The dangerous row is the first. Because `Directory.Build.props` also sets
`PredefinedCulturesOnly=false`, constructing a culture in invariant mode **succeeds** instead of
throwing `CultureNotFoundException`, and hands back an object populated with invariant data. Number
parsing and formatting then produce wrong values with no error, and culture-sensitive sort order
silently becomes ordinal.

Encoding is not affected — UTF-8 round-trips correctly in both modes.

### Version floor

The runtime accepts `libicuuc.so.60` and above (`MinICUVersion` in `pal_icushim.c`). The prerequisite
checker enforces the same floor, so a host carrying only an older ICU is reported missing rather than
passing and then aborting at startup. RHEL/CentOS 7 ships ICU 50 and is affected.

ICU tracks its own release train, so the SONAME digit varies widely by distribution — `.so.74` on
Ubuntu 24.04, `.so.76` on Alpine, `.so.77` on Fedora, `.so.78` on openSUSE. There is no stable
package alias on Debian and Ubuntu, which is why the checker resolves the name via `apt-cache`
instead of hardcoding one.

Only `libicuuc` and `libicui18n` are used; those are the two names
`libSystem.Globalization.Native.so` loads. `libicudata` arrives as a dependency of `libicuuc`, and
`libicuio`/`libicutu`/`libicutest` are never referenced. Every distribution ships all of them in a
single package, so installing ICU at all satisfies both.

## tzdata

The event scheduler resolves configured zone IDs through `TimeZoneInfo`, which reads
`/usr/share/zoneinfo` on Linux. This is separate from ICU: it is data, not a library, so no loader
probe finds it, and slim container images routinely omit it.

Without tzdata every lookup except `UTC` throws:

```
TimeZoneNotFoundException: The time zone ID 'America/New_York' was not found on the local computer.
```

`TimeZoneInfo.GetSystemTimeZones()` returns 1 entry instead of ~419, and `TimeZoneInfo.Local` falls
back to UTC.

### There is no per-zone subset

Distributions do not package individual zones — it is one `tzdata` package, about 2 MB installed for
the full set. Subsetting is not worth pursuing.

The one split that does exist is **`tzdata-legacy`** on Debian 12 and Ubuntu 24.04, which carries the
deprecated aliases. With plain `tzdata` alone:

| Zone ID | `tzdata` | `tzdata-legacy` |
|---|---|---|
| `America/New_York` | present | — |
| `Europe/Kyiv` | present | — |
| `EST5EDT` | present | — |
| `US/Eastern` | **missing** | present |
| `Asia/Calcutta` | **missing** | present |

So a shard configured with a legacy alias such as `US/Eastern` throws on a current Debian or Ubuntu
even though tzdata is installed. Either install `tzdata-legacy` or switch the configured value to the
canonical ID (`America/New_York`, `Asia/Kolkata`).

`TZDIR` is honoured if the data lives somewhere non-standard.

## How the check works

`--check-prereqs` asks the loader directly — `NativeLibrary.TryLoad` on the unversioned name, then
`libfoo.so.N` descending through the accepted range.

It deliberately does not consult a package database or `ldconfig -p`. Both answer a different
question than "will `dlopen` succeed":

- Package queries need a hardcoded name, which does not exist for ICU.
- `ldconfig`'s cache can be stale, omits `LD_LIBRARY_PATH`, and carries no version information to
  enforce the ICU floor against. On musl it exits successfully while producing nothing usable.
