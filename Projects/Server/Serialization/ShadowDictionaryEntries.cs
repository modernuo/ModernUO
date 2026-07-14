/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ShadowDictionaryEntries.cs                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server;

/// <summary>
/// Mirrors the field layout of <c>Dictionary&lt;Serial, TValue&gt;.Entry</c> so serialization
/// workers can scan a dictionary's backing entries array directly, in parallel, without the
/// main thread enumerating and handing off every entity.
/// The CLR's auto-layout algorithm is deterministic for identical field sequences, so this
/// struct lays out identically to the runtime's private Entry struct — and
/// <see cref="ShadowDictionaryEntries.Supported"/> proves that empirically at startup before
/// any code reads through it. If validation fails on a future runtime, callers fall back to
/// the enumerate-and-push path.
/// A free or never-used slot always has a null value (Dictionary clears values of
/// reference-type TValue on remove to release references), so occupancy is exactly
/// <c>Value != null</c>.
/// </summary>
internal struct ShadowEntry<TValue>
{
    // Field order must mirror S.P.CoreLib Dictionary<TKey,TValue>.Entry: hashCode, next, key, value
    public uint HashCode;
    public int Next;
    public Serial Key;
    public TValue Value;
}

internal static class ShadowDictionaryEntries
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ShadowDictionaryEntries));

    /// <summary>
    /// True when this runtime's Dictionary entry layout matches <see cref="ShadowEntry{TValue}"/>,
    /// proven by validation at startup. All reference-type TValue instantiations share one
    /// canonical layout, so a single validation covers every entity dictionary.
    /// </summary>
    internal static readonly bool Supported = Validate();

    internal static FieldInfo GetEntriesField<TValue>() where TValue : class =>
        typeof(Dictionary<Serial, TValue>).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);

    private sealed class ValidationValue
    {
        public int Id;
    }

    private static bool Validate()
    {
        try
        {
            var field = GetEntriesField<ValidationValue>();
            if (field == null)
            {
                logger.Warning("Dictionary._entries not found; parallel save iteration disabled.");
                return false;
            }

            // A compacting GC between snapshotting reference bits and scanning them can only
            // produce a false negative, so retry a few times before falling back.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (RunValidationPass(field))
                {
                    return true;
                }
            }

            logger.Warning("Dictionary entry layout mismatch; parallel save iteration disabled.");
            return false;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Dictionary entry layout validation failed; parallel save iteration disabled.");
            return false;
        }
    }

    /// <summary>
    /// Measures the true element stride of the runtime's Entry struct by allocating a large
    /// array of it and reading the precise allocated byte count. Verifying the stride first
    /// guarantees every subsequent shadow read is in-bounds of the entries array even if the
    /// field layout were wrong.
    /// </summary>
    private static bool StrideMatches(Type entryType)
    {
        const int probeLength = 64 * 1024;
        const int arrayHeaderSize = 24; // 64-bit: sync block + method table + length + padding

        // Warm the reflection/allocation path so the measured delta contains only the probe.
        Array.CreateInstance(entryType, 1);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var probe = Array.CreateInstance(entryType, probeLength);
        var delta = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(probe);

        // Alignment slack is < 8 bytes on a 512KB+ allocation, so integer division is exact.
        var actualStride = (delta - arrayHeaderSize) / probeLength;

        return actualStride == Unsafe.SizeOf<ShadowEntry<ValidationValue>>();
    }

    /// <summary>
    /// Proves the layout without ever materializing a managed reference through the shadow
    /// view: value slots are compared as raw pointer bits (nint). Only after the stride and
    /// every key and value-pointer of a churned, resized, freelist-exercised dictionary match
    /// is the layout trusted for typed reads.
    /// </summary>
    private static bool RunValidationPass(FieldInfo field)
    {
        const int count = 1000;

        var dict = new Dictionary<Serial, ValidationValue>();
        var rng = new System.Random(0x5EED);
        var inserted = new List<Serial>(count);

        // Adds with interleaved removes and re-adds: exercises resizes and freelist reuse so
        // freed slots (which production skips via null values) are present in the array.
        for (var i = 0; i < count; i++)
        {
            var serial = (Serial)(uint)rng.Next(1, int.MaxValue);
            if (dict.TryAdd(serial, new ValidationValue { Id = i }))
            {
                inserted.Add(serial);
            }

            if (i % 3 == 2)
            {
                var victim = inserted[rng.Next(inserted.Count)];
                if (dict.Remove(victim))
                {
                    inserted.Remove(victim);
                }
            }
        }

        if (field.GetValue(dict) is not Array entriesObj || !StrideMatches(entriesObj.GetType().GetElementType()!))
        {
            return false;
        }

        // Snapshot expected key -> value-pointer-bits pairs before scanning, so no allocation
        // happens between reading the real references and reading the shadow view.
        var expected = new Dictionary<Serial, nint>(dict.Count);
        foreach (var (key, value) in dict)
        {
            var v = value;
            expected[key] = Unsafe.As<ValidationValue, nint>(ref v);
        }

        var entries = Unsafe.As<ShadowEntry<ValidationValue>[]>(entriesObj);
        var length = entriesObj.Length;
        var matched = 0;

        ref var entry = ref MemoryMarshal.GetArrayDataReference(entries);

        for (var i = 0; i < length; i++, entry = ref Unsafe.Add(ref entry, 1))
        {
            // Read the value slot as pointer bits only — never as a reference — so a wrong
            // field offset cannot fabricate a managed reference for the GC to trip over.
            var bits = Unsafe.As<ValidationValue, nint>(ref entry.Value);

            if (bits == 0)
            {
                continue; // free or never-used slot
            }

            if (!expected.TryGetValue(entry.Key, out var expectedBits) || bits != expectedBits)
            {
                return false;
            }

            matched++;
        }

        return matched == dict.Count;
    }
}
