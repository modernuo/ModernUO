/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SlotStatus.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;

namespace Server;

/// <summary>
/// What a save worker decided for one occupied slot, packed into the <c>int</c> per slot that
/// the worker logs during the freeze and the snapshot writer decodes in the same order:
/// <list type="bullet">
/// <item><c>&gt;= 0</c>: emitted, the value is the record length in the worker heap (a zero-length
/// record still gets an index entry, which the loader treats as a deletion, exactly as before).</item>
/// <item><c>-1</c>: copied, the writer copies the record from the entity's <see cref="ISerializable.SavePlacement" />.</item>
/// <item><c>&lt;= -2</c>: emitted for verification, length <c>-2 - value</c>; the writer compares it with the previous record.</item>
/// <item><see cref="int.MinValue" />: skipped, no record (<see cref="ISerializable.SkipSerialization" />).</item>
/// </list>
/// </summary>
public static class SlotStatus
{
    public const int Skipped = int.MinValue;
    public const int Copied = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Emitted(int length) => length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Verify(int length) => -2 - length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVerify(int status) => status <= -2 && status != Skipped;

    /// <summary>Record length for an emitted or verify status; zero for skipped and copied.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(int status) =>
        status >= 0 ? status : status == Copied || status == Skipped ? 0 : -2 - status;
}
