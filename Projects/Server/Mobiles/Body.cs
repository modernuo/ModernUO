/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Body.cs                                                         *
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
using System.IO;
using System.Runtime.CompilerServices;
using Server.Logging;

namespace Server;

public enum BodyType : byte
{
    Empty,
    Monster,
    Sea,
    Animal,
    Human,
    Equipment
}

public readonly struct Body : IEquatable<object>, IEquatable<Body>, IEquatable<int>,
    IComparable<int>, IComparable<Body>, ISpanParsable<Body>
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Body));

    private static readonly BodyType[] m_Types = Array.Empty<BodyType>();

    static Body()
    {
        if (!File.Exists("Data/bodyTable.cfg"))
        {
            logger.Error("Data/bodyTable.cfg does not exist.");
            return;
        }

        using var ip = new StreamReader("Data/bodyTable.cfg");
        m_Types = new BodyType[0x1000];

        string line;

        while ((line = ip.ReadLine()) != null)
        {
            if (line.Length == 0 || line.StartsWithOrdinal("#"))
            {
                continue;
            }

            var split = line.Split('\t');

            if (int.TryParse(split[0], out var bodyID) && Enum.TryParse(split[1], true, out BodyType type) &&
                bodyID >= 0 &&
                bodyID < m_Types.Length)
            {
                m_Types[bodyID] = type;
            }
            else
            {
                logger.Warning("Invalid bodyTable entry: {Entry}", line);
            }
        }
    }

    public Body(int bodyID) => BodyID = bodyID;

    public BodyType Type => BodyID >= 0 && BodyID < m_Types.Length ? m_Types[BodyID] : BodyType.Empty;

    public bool IsHuman => BodyID >= 0
                           && BodyID < m_Types.Length
                           && m_Types[BodyID] == BodyType.Human
                           && BodyID != 402
                           && BodyID != 403
                           && BodyID != 607
                           && BodyID != 608
                           && BodyID != 694
                           && BodyID != 695
                           && BodyID != 970;

    public bool IsGargoyle => BodyID is 666 or 667 or 694 or 695;

    public bool IsMale => BodyID is 183 or 185 or 400 or 402 or 605 or 607 or 666 or 694 or 750;

    public bool IsFemale => BodyID is 184 or 186 or 401 or 403 or 606 or 608 or 667 or 695 or 751;

    public bool IsGhost => BodyID is 402 or 403 or 607 or 608 or 694 or 695 or 970;

    public bool IsMonster => BodyID >= 0
                             && BodyID < m_Types.Length
                             && m_Types[BodyID] == BodyType.Monster;

    public bool IsAnimal => BodyID >= 0
                            && BodyID < m_Types.Length
                            && m_Types[BodyID] == BodyType.Animal;

    public bool IsEmpty => BodyID >= 0
                           && BodyID < m_Types.Length
                           && m_Types[BodyID] == BodyType.Empty;

    public bool IsSea => BodyID >= 0
                         && BodyID < m_Types.Length
                         && m_Types[BodyID] == BodyType.Sea;

    public bool IsEquipment => BodyID >= 0
                               && BodyID < m_Types.Length
                               && m_Types[BodyID] == BodyType.Equipment;

    public int BodyID { get; }

    public static implicit operator int(Body a) => a.BodyID;

    public static implicit operator Body(int a) => new(a);

    public override string ToString() => $"0x{BodyID:X}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => BodyID.GetHashCode();

    public override bool Equals(object o) => o is Body b && b.BodyID == BodyID;

    public bool Equals(Body b) => b.BodyID == BodyID;

    public bool Equals(int number) => number == BodyID;

    public static bool operator ==(Body l, Body r) => l.BodyID == r.BodyID;

    public static bool operator !=(Body l, Body r) => l.BodyID != r.BodyID;

    public static bool operator >(Body l, Body r) => l.BodyID > r.BodyID;

    public static bool operator >=(Body l, Body r) => l.BodyID >= r.BodyID;

    public static bool operator <(Body l, Body r) => l.BodyID < r.BodyID;

    public static bool operator <=(Body l, Body r) => l.BodyID <= r.BodyID;

    public int CompareTo(Body other) => BodyID.CompareTo(other.BodyID);

    public int CompareTo(int other) => BodyID.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Body Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Body Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Body result) =>
        TryParse(s.AsSpan(), provider, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Body Parse(ReadOnlySpan<char> s, IFormatProvider provider) => Utility.ToInt32(s);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Body result)
    {
        if (Utility.ToInt32(s, out var value))
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }
}
