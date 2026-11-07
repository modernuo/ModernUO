# RngLab — PRNG evaluation for `BuiltInRng`

Reference lab, not part of the solution. It evaluates replacing the shared `System.Random` (Xoshiro256**)
behind `Utility.Random*`. The motivating complaint: one global PRNG serves every consumer, so players
see long runs of crafting failures and bad (or too-good) treasure loot.

```sh
dotnet run -c Release -- verify                 # reference vectors
dotnet run -c Release -- streaks [n]            # run-length test vs. theory + bad-luck-protection demo
DOTNET_TieredCompilation=0 DOTNET_JitDisasm=NextUInt64 dotnet run -c Release -- disasm
dotnet run -c Release -- bench --filter '*' --join --affinity 4   # pin a core; hybrid CPUs are noisy otherwise
```

## Conclusion

**The streaks aren't a generator defect, and changing the PRNG won't change what players see.**
`streaks` rolls 200M times per generator and compares failure-run lengths to the exact geometric law
(`P(len = k) = q^(k-1)·p`). A generator that "falls into close repeats" would show an excess of long runs,
which would appear as a large positive z.

| p (success) | Xoshiro256** | PCG64-DXSM | SFC64 | Xoshiro, shared (0–63 foreign draws between rolls) |
|---|---|---|---|---|
| 0.90 | z = −0.97 | −0.62 | +0.41 | +0.11 |
| 0.50 | +1.54 | +1.02 | −1.25 | −0.41 |
| 0.02 | −0.91 | −0.28 | −0.80 | −0.21 |

- Every generator, the shared global instance included, matches theory within ±2σ.
- At p = 0.02, every generator produced a 700–790-roll dry spell somewhere in the run. That is what a 2% chance does.
- Sharing one instance just subsamples one stream, so it can't create clumping.

## Options that *do* change the experience (not implemented)

These are design changes, not RNG changes. They need small per-player, per-system state (for example a
craft-failure counter or a chest-tier counter), so they belong behind opt-in `Utility` helpers. Content
authors then don't have to hand-build loot tables to work around streaks.

- **Bad-luck protection (pseudo-random distribution, Dota-style).** The chance is `C·(failures since last success + 1)`,
  with `C` solved so the long-run rate equals the nominal `p`. Measured at p = 0.02 over 200M rolls:

  | | rate | worst dry spell | dry spells ≥150 per 1k successes |
  |---|---|---|---|
  | plain | 0.01998 | 784 | 48.4 |
  | PRD (C = 0.000620) | 0.02001 | 221 | 0.69 |

  PRD also makes back-to-back successes less likely, so the "too good" side is bounded as well.
- **Shuffle bags** for loot tiers: draw without replacement from a bag holding N commons, M rares and so on, then refill.
  The exact distribution is guaranteed per bag.
- **Pity timers:** a guaranteed success after K failures. This is the simplest option, but it creates a visible
  breakpoint that players learn.
- **Audit the loot code before blaming the RNG.** Nested rolls, inclusive/exclusive range mix-ups
  (`Random(min, count)` vs `RandomMinMax`) and integer truncation bias results for real. This has not been audited yet.

## Per-system or per-player streams: jumps vs. seeding

Per-player or per-system streams **don't change run-length statistics**. Each stream is still i.i.d. and
still streaky. What they buy:

- **Isolation:** one system's draw count can't shift another's sequence.
- **Reproducibility:** derive the stream from `(worldSeed, serial, systemId)` and replay it in tests.

**Jumps vs. seeding a new instance:**
- A jump (for example xoshiro256's 2^128 jump) gives *provably* disjoint substreams from one master seed. That matters
  for small-state generators, and when overlap must be impossible rather than just improbable.
- With 256 bits of state seeded through SplitMix64, overlap of n streams each using L outputs has probability about
  n²·L / 2^256. For 10^6 streams × 2^50 draws that is about 2^-166, which is nil.
- SplitMix64 is a bijection on its counter, so distinct seeds always give distinct initial states.
- **Conclusion:** jumps buy nothing at our scale. Seeding is simpler, and it allows deriving a stream from an ID without
  storing or serializing state.

**Caveat on "just seed another `System.Random`":**
- `new System.Random(seed)` is **not** Xoshiro. A seeded constructor selects the legacy .NET Framework subtractive
  generator (`Net5CompatSeedImpl`). It is slower, and its `Next(max)` is `(int)(Sample() * max)` over a 31-bit sample.
- Only the unseeded `new Random()` gets Xoshiro, and that is OS-seeded and can't be reproduced.
- Per-stream instances therefore need our own generator type. Any of the ones here would do, or the old Xoroshiro class.
- Note: `FishingTests` seeds `BuiltInRng.Generator = new System.Random(1)`, so it runs the legacy algorithm. That's fine for a test.

LXM (`L64X128Mix`, the paper's recommendation) is built for exactly this. `split()` is cheap,
and distinct LCG additive constants plus the mixer decorrelate the streams. Per the authors, the independence is
believed but not proven. For one single-threaded stream, the paper itself recommends Xoroshiro128++ instead.

## Generators

| Generator | Construction | Verified against |
|---|---|---|
| `Xoshiro256SS` | what unseeded `System.Random` uses | — |
| `Xoroshiro128PP` | Vigna; the LXM paper's single-thread pick | — |
| `Sfc64` | Doty-Humphrey chaotic + counter | — |
| `L64X128Mix` | 64-bit LCG + raw xoroshiro128, summed, then the lea64 mixer (Steele & Vigna 2021) | lxm_random.hpp algorithm (Python port) |
| `Pcg64Dxsm` | 128-bit LCG with the 64-bit "cheap multiplier", DXSM output | numpy 2.5.2 `PCG64DXSM.random_raw` |

LXM is **not** "Xoshiro + MurmurHash3":
- the xoroshiro128 part is raw (unscrambled);
- a 64-bit LCG (`M = 0xd1342543de82ef95`, odd per-instance `a`) is added to it;
- the mixer is Doug Lea's `lea64` (`z ^= z>>32; z *= 0xdaba0b6eb09322e3`, twice, then `z ^= z>>32`), a murmur3-style variant with different constants.

It is a different generator from Xoshiro256**, with no shared sequence. Its mixer removes low-bit flaws
that PractRand finds in the raw LCG+XBG sum; it is not aimed at streaks.

### The originally proposed PCG64-DXSM had problems

1. It doesn't compile: C# has no 128-bit literals, so `0x2360…F645UL` must be `new UInt128(hi, lo)`.
2. It isn't PCG64-DXSM. It steps the state with the 128-bit PCG64 (XSL-RR) multiplier. DXSM uses the 64-bit cheap
   multiplier for both the step and the output, so it can't be checked against reference vectors, and it was the slowest variant (≈2.0 ns/draw).
3. It is a **mutable struct**. A copy (a `readonly` field, or passing it by value) replays outputs, which is the very "repeat" bug we're trying to avoid.
4. It accepts only a 64-bit seed for 128 bits of state.

### `Pcg64Dxsm` lowering (.NET 10, x64; `DOTNET_JitDisasm`)

- `Math.BigMul(ulong, ulong, out ulong)` already compiles to `MULX` when BMI2 is present. An explicit
  `Bmi2.X64.MultiplyNoFlags(a, b, &lo)` produces the same code.
- Every overload that hands back the low half (`out`, pointer, or the `UInt128`-returning `Math.BigMul`) spills that half
  through a stack slot. The high-only `MultiplyNoFlags(a, b)` plus a separate `IMUL` avoids the spill, but measured *no faster*.
- The win that does measure is a **compile-time-constant increment** (numpy's default stream, 109). Its high half is 0,
  so the add folds to an immediate. A runtime 128-bit increment costs about 15%, and a `UInt128` state about 30%.
- The carry still costs `cmp/setb/movzx/add`, because C# has no add-with-carry intrinsic.
- The output is mixed from the pre-advance state, so the two output `IMUL`s are off the state's dependency chain.

## Benchmarks

Measured on an i9-12900K, pinned to one P-core (`--affinity 4`), .NET 10, while the machine was otherwise loaded.
The numbers are ns per op, with 1024 draws per op.

Raw `NextUInt64` (ns per number = op / 1024):

| Generator | ns/op | ns/draw |
|---|---:|---:|
| Xoshiro256** | 817 | 0.80 |
| SFC64 | 821 | 0.80 |
| Xoroshiro128++ | 892 | 0.87 |
| Pcg64Dxsm | 1,126 | 1.10 |
| L64X128Mix | 1,234 | 1.20 |

API shape, the way game code calls it:

| Call | `System.Random` (today) | static Xoshiro | static PCG | static LXM |
|---|---:|---:|---:|---:|
| `Next(100)` ns/draw | 1.13 | 0.90 | 1.26 | 1.39 |
| `NextDouble()` ns/draw | 1.32 | 1.23 | 1.56 | 1.64 |

All candidates are within about 0.4 ns/draw of each other. At this server's call rates that is noise, so **speed
doesn't decide this**. Dropping the `System.Random` wrapper (virtual dispatch into `XoshiroImpl`) for a static
generator is the only measurable improvement over today's `BuiltInRng`.
