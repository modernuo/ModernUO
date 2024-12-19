/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Localization.cs                                                 *
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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Buffers;

namespace Server;

public static class Localization
{
    private const bool _loadLocalizationOnStartup = false;
    public const string FallbackLanguage = "enu";

    private static Dictionary<int, LocalizationEntry> _fallbackEntries;
    private static readonly Dictionary<string, Dictionary<int, LocalizationEntry>> _localizations = new();

    public static void Configure()
    {
        if (_loadLocalizationOnStartup)
        {
            foreach (var file in Core.FindDataFileByPattern("cliloc.*"))
            {
                var fi = new FileInfo(file);
                LoadClilocs(fi.Extension.ToLowerInvariant(), file);
            }
        }
    }

    public static void Add(string lang, int number, string text)
    {
        lang = lang?.ToLower() ?? FallbackLanguage;
        var entry = new LocalizationEntry(lang, number, text);
        if (!_localizations.TryGetValue(lang, out var entries))
        {
            entries = new Dictionary<int, LocalizationEntry>();
            _localizations[lang] = entries;
            if (lang == FallbackLanguage)
            {
                _fallbackEntries ??= entries;
            }
        }

        entries.Add(number, entry);
    }

    public static bool Remove(string lang, int number)
    {
        lang = lang?.ToLower() ?? FallbackLanguage;
        if (!_localizations.TryGetValue(lang, out var entries) || !entries.Remove(number))
        {
            return false;
        }

        if (entries.Count == 0)
        {
            _localizations.Remove(lang);
        }

        return true;
    }

    public static void Clear()
    {
        _localizations.Clear();
        _fallbackEntries = null;
    }

    public static Dictionary<int, LocalizationEntry> LoadClilocs(string lang) =>
        LoadClilocs(lang, Core.FindDataFile($"cliloc.{lang}", false));

    private static Dictionary<int, LocalizationEntry> LoadClilocs(string lang, string file)
    {
        lang = lang?.ToLower() ?? FallbackLanguage;
        Dictionary<int, LocalizationEntry> entries = _localizations[lang] = new Dictionary<int, LocalizationEntry>();
        if (lang == FallbackLanguage)
        {
            _fallbackEntries = entries;
        }

        if (File.Exists(file))
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[6];
            _ = fs.Read(header);

            byte[] data;
            BufferReader br;
            if (BinaryPrimitives.ReadInt32LittleEndian(header) != 2 || BinaryPrimitives.ReadInt16LittleEndian(header[4..]) != 1)
            {
                // Skip header
                fs.Position = 4;
                data = BwtDecompress.Decompress(fs, (int)fs.Length - 4);
                br = new BufferReader(data);

                var header2 = br.ReadInt();   // Header 2
                var header1 = br.ReadShort(); // Header 1

                if (header2 != 2 || header1 != 1)
                {
                    throw new Exception($"Invalid cliloc header in {file}");
                }
            }
            else
            {
                data = GC.AllocateUninitializedArray<byte>((int)fs.Length - 6);
                _ = fs.Read(data);
                br = new BufferReader(data);
            }

            byte[] buffer = null;
            while (br.Position < data.Length)
            {
                var number = br.ReadInt();
                var flag = br.ReadByte(); // Original, Custom, Modified
                var length = br.ReadShort();

                if (buffer == null || buffer.Length < length)
                {
                    buffer = GC.AllocateUninitializedArray<byte>(length);
                }

                br.Read(buffer.AsSpan(0, length));

                var text = Encoding.UTF8.GetString(buffer.AsSpan(0, length));
                entries[number] = new LocalizationEntry(lang, number, text);
            }
        }

        return entries;
    }

    /// <summary>
    /// Returns the original text for a localization entry.
    /// </summary>
    /// <param name="number">Localization number</param>
    /// <param name="lang">Language in ISO 639‑2 format</param>
    /// <returns>Original text for the localization entry</returns>
    public static string GetText(int number, string lang = FallbackLanguage) =>
        TryGetLocalization(lang, number, out var entry) ? entry.Text : null;

    /// <summary>
    /// Gets a localization entry using the <see cref="FallbackLanguage" />.
    /// </summary>
    /// <param name="number">Localization number</param>
    /// <param name="entry">Localization entry retrieved</param>
    /// <returns>True if the entry exists, otherwise false.</returns>
    public static bool TryGetLocalization(int number, out LocalizationEntry entry) =>
        TryGetLocalization(FallbackLanguage, number, out entry);

    /// <summary>
    /// Gets a localization entry.
    /// </summary>
    /// <param name="lang">Language in ISO 639-2 format</param>
    /// <param name="number">Localization number</param>
    /// <param name="entry">Localization entry retrieved</param>
    /// <returns>True if the entry exists, otherwise false.</returns>
    public static bool TryGetLocalization(string lang, int number, out LocalizationEntry entry)
    {
        lang = lang?.ToLower() ?? FallbackLanguage;
        if (lang != FallbackLanguage)
        {
            if (!_localizations.TryGetValue(lang, out var entries))
            {
                entries = LoadClilocs(lang);
            }

            if (entries.TryGetValue(number, out entry))
            {
                return true;
            }
        }

        _fallbackEntries ??= LoadClilocs(FallbackLanguage);
        return _fallbackEntries.TryGetValue(number, out entry);
    }

    /// <summary>
    /// Creates a formatted string of the localization entry using the specified language.
    /// Uses string interpolation under the hood. This method is preferably relative to the object array method signature.
    /// Example:
    /// Localization.Format(1073841, "jpn", $"{totalItems}{maxItems}{totalWeight}");
    /// </summary>
    /// <param name="lang">Language in ISO 639-2 format</param>
    /// <param name="number">Localization number</param>
    /// <param name="handler">interpolated string handler used by the compiler as a string builder during compilation</param>
    /// <returns>A copy of the localization text where the placeholder arguments have been replaced with string representations of the provided interpolation arguments</returns>
    public static PooledArraySpanFormattable Format(
        int number, string lang,
        [InterpolatedStringHandlerArgument("number", "lang")]
        ref LocalizationEntry.LocalizationInterpolationHandler handler
    )
    {
        var chars = handler.ToPooledArray(out var length);
        handler = default; // Defensive clear
        return new PooledArraySpanFormattable(chars, length);
    }

    /// <summary>
    /// Creates a formatted string of the localization entry using the <see cref="FallbackLanguage" />.
    /// Uses string interpolation under the hood. This method is preferably relative to the object array method signature.
    /// Example:
    /// Localization.Format(1073841, $"{totalItems}{maxItems}{totalWeight}");
    /// </summary>
    /// <param name="number">Localization number</param>
    /// <param name="handler">interpolated string handler used by the compiler as a string builder during compilation</param>
    /// <returns>A copy of the localization text where the placeholder arguments have been replaced with string representations of the provided interpolation arguments</returns>
    public static PooledArraySpanFormattable Format(
        int number,
        [InterpolatedStringHandlerArgument("number")]
        ref LocalizationEntry.LocalizationInterpolationHandler handler
    )
    {
        var chars = handler.ToPooledArray(out var length);
        handler = default; // Defensive clear
        return new PooledArraySpanFormattable(chars, length);
    }
}
