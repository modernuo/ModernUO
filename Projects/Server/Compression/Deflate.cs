/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Deflate.cs                                                      *
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
using System.IO.Compression;

namespace Server.Compression;

public static class Deflate
{
    [ThreadStatic]
    private static LibDeflateBinding _standard;

    [ThreadStatic]
    private static LibDeflateBinding _maximum;

    public static LibDeflateBinding Standard => _standard ??= new LibDeflateBinding();

    // Best-ratio compressor. Construction allocates a native libdeflate compressor, so it is
    // cached per thread like Standard and reused. Decompression is level-independent, so the
    // decompress path can use either accessor. libdeflate is not thread-safe — hence ThreadStatic.
    public static LibDeflateBinding Maximum => _maximum ??= new LibDeflateBinding(LibDeflateCompressionLevel.VeryHigh);
}
